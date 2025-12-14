using ConsumerApi;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// optional: raise logging verbosity while troubleshooting
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<MetricsStore>();
var initialConfig = builder.Configuration
    .GetSection("ResilienceConfig")
    .Get<ResilienceConfig>() ?? new ResilienceConfig();
builder.Services.AddSingleton(new ResilienceConfigStore(initialConfig));

// Prometheus metrics (global)
var totalCalls = Metrics.CreateCounter("pollydemo_consumer_total_calls", "Total number of calls to /consume");
var producerFailures = Metrics.CreateCounter("pollydemo_producer_failures_total", "Total failed responses from Producer");
var retriesCounter = Metrics.CreateCounter("pollydemo_retries_total", "Total retries fired by Consumer");
var circuitStateGauge = Metrics.CreateGauge("pollydemo_circuit_state", "Circuit state gauge: 0=Closed,0.5=HalfOpen,1=Open");
var circuitOpenCounter = Metrics.CreateCounter("pollydemo_circuit_open_total", "Total number of times the circuit breaker opened");
var requestDuration = Metrics.CreateHistogram("pollydemo_consumer_request_duration_seconds", "Request duration histogram for /consume", new Prometheus.HistogramConfiguration
{
    Buckets = Histogram.ExponentialBuckets(start: 0.01, factor: 2, count: 10)
});

builder.Services.AddHttpClient("ProducerClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProducerApi:BaseUrl"]!);
})
.AddResilienceHandler("producer-pipeline", (pipelineBuilder, context) =>
{
    var serviceProvider = context.ServiceProvider;
    var store = serviceProvider.GetRequiredService<ResilienceConfigStore>();
    var cfg = store.GetConfig();

    // Retry strategy
    pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = cfg.RetryCount,
        Delay = TimeSpan.FromMilliseconds(cfg.RetryBackoffMilliseconds.Count > 0 ? cfg.RetryBackoffMilliseconds[0] : 200),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = args =>
        {
            return new ValueTask<bool>(
                args.Outcome.Exception != null ||
                (args.Outcome.Result is HttpResponseMessage resp && !resp.IsSuccessStatusCode)
            );
        },
        OnRetry = args =>
        {
            var metrics = serviceProvider.GetRequiredService<MetricsStore>();
            metrics.RecordRetry();
            retriesCounter.Inc();
            serviceProvider.GetRequiredService<ILogger<Program>>()
                .LogWarning("Retry {RetryNumber} after {Delay}ms: {Reason}",
                    args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message ?? (args.Outcome.Result as HttpResponseMessage)?.ReasonPhrase);
            return ValueTask.CompletedTask;
        }
    });

    // Circuit breaker strategy
    pipelineBuilder.AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5,
        MinimumThroughput = cfg.ExceptionsAllowedBeforeBreaking,
        SamplingDuration = TimeSpan.FromSeconds(cfg.DurationOfBreakInSeconds),
        BreakDuration = TimeSpan.FromSeconds(cfg.DurationOfBreakInSeconds),
        ShouldHandle = args =>
        {
            return new ValueTask<bool>(
                args.Outcome.Exception != null ||
                (args.Outcome.Result is HttpResponseMessage resp && !resp.IsSuccessStatusCode)
            );
        },
        OnOpened = args =>
        {
            var metrics = serviceProvider.GetRequiredService<MetricsStore>();
            metrics.SetCircuitState("Open");
            metrics.RecordCircuitOpen();
            circuitStateGauge.Set(1);
            circuitOpenCounter.Inc();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var status = (args.Outcome.Result as HttpResponseMessage)?.StatusCode.ToString() ?? args.Outcome.Exception?.GetType().Name ?? "Unknown";
            var reason = args.Outcome.Exception?.Message ?? (args.Outcome.Result as HttpResponseMessage)?.ReasonPhrase ?? "none";
            var openCount = metrics.CircuitOpenCount;
            logger.LogWarning("Circuit opened for {BreakDuration}s (count={OpenCount}): Status={Status}, Reason={Reason}",
                args.BreakDuration.TotalSeconds, openCount, status, reason);
            return ValueTask.CompletedTask;
        },
        OnClosed = args =>
        {
            var metrics = serviceProvider.GetRequiredService<MetricsStore>();
            metrics.SetCircuitState("Closed");
            metrics.SetLastReset(DateTime.UtcNow);
            circuitStateGauge.Set(0);
            serviceProvider.GetRequiredService<ILogger<Program>>()
                .LogWarning("Circuit closed");
            return ValueTask.CompletedTask;
        },
        OnHalfOpened = args =>
        {
            var metrics = serviceProvider.GetRequiredService<MetricsStore>();
            metrics.SetCircuitState("HalfOpen");
            circuitStateGauge.Set(0.5);
            serviceProvider.GetRequiredService<ILogger<Program>>()
                .LogInformation("Circuit is half-open and testing a request.");
            return ValueTask.CompletedTask;
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Expose Prometheus metrics endpoint and enable HTTP metrics (captures basic http server metrics)
app.UseMetricServer("/prometheus");
app.UseHttpMetrics();

app.UseHttpsRedirection();

// Endpoints
app.MapGet("/consume", async (IHttpClientFactory factory, MetricsStore metrics) =>
{
    Console.WriteLine($"---------------------->[ConsumerApi] /consume called.");
    metrics.RecordCall();
    totalCalls.Inc();

    var client = factory.CreateClient("ProducerClient");

    using (requestDuration.NewTimer())
    {
        var response = await client.GetAsync("/produce");

        if (!response.IsSuccessStatusCode)
        {
            producerFailures.Inc();
            return Results.Problem(
                statusCode: (int)response.StatusCode,
                detail: "Producer error");
        }

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"---------------------->[ConsumerApi] Received from Producer: {content}");
        return Results.Ok(content);
    }
});

app.MapGet("/metrics", (MetricsStore metrics) =>
{
    Console.WriteLine($"---------------------->[ConsumerApi] Metrics requested: {System.Text.Json.JsonSerializer.Serialize(metrics)}.");
    return Results.Ok(metrics);
});

app.MapGet("/config", (ResilienceConfigStore store) =>
{
    var config = store.GetConfig();
    Console.WriteLine($"---------------------->[ConsumerApi] Config requested: {System.Text.Json.JsonSerializer.Serialize(config)}.");
    return config;
});

app.MapPut("/config", (ResilienceConfig newCfg, ResilienceConfigStore store) =>
{
    Console.WriteLine($"---------------------->[ConsumerApi] Config update received: {System.Text.Json.JsonSerializer.Serialize(newCfg)}.");
    store.UpdateConfig(newCfg);
    return Results.NoContent();
});

await app.RunAsync();