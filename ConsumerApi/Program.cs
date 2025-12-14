using ConsumerApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Polly;
using Polly.Extensions.Http;
using Prometheus;
using System;

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
var requestDuration = Metrics.CreateHistogram("pollydemo_consumer_request_duration_seconds", "Request duration histogram for /consume", new Prometheus.HistogramConfiguration
{
    Buckets = Histogram.ExponentialBuckets(start: 0.01, factor: 2, count: 10)
});

builder.Services.AddHttpClient("ProducerClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProducerApi:BaseUrl"]!);
})
.AddPolicyHandler((provider, request) =>
{
    var store = provider.GetRequiredService<ResilienceConfigStore>();
    var cfg = store.GetConfig();

    // Create the policies
    var circuitBreaker = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            cfg.ExceptionsAllowedBeforeBreaking,
            TimeSpan.FromSeconds(cfg.DurationOfBreakInSeconds),
            onBreak: (outcome, breakDelay) =>
            {
                var metrics = provider.GetRequiredService<MetricsStore>();
                metrics.SetCircuitState("Open");
                circuitStateGauge.Set(1);

                var logger = provider.GetRequiredService<ILogger<Program>>();
                // outcome.Exception may be null when the failure is an HTTP response (5xx).
                var status = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.GetType().Name ?? "Unknown";
                var reason = outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "none";
                logger.LogWarning("Circuit opened for {Delay}s: Status={Status}, Reason={Reason}",
                    breakDelay.TotalSeconds, status, reason);
            },
            onReset: () =>
            {
                var metrics = provider.GetRequiredService<MetricsStore>();
                metrics.SetCircuitState("Closed");
                metrics.SetLastReset(DateTime.UtcNow);
                circuitStateGauge.Set(0);
                provider.GetRequiredService<ILogger<Program>>()
                    .LogWarning("Circuit closed");
            },
            onHalfOpen: () =>
            {
                var metrics = provider.GetRequiredService<MetricsStore>();
                metrics.SetCircuitState("HalfOpen");
                // use 0.5 to represent half-open in the gauge
                circuitStateGauge.Set(0.5);
                provider.GetRequiredService<ILogger<Program>>()
                    .LogInformation("Circuit is half-open and testing a request.");
            });

    var retry = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            cfg.RetryCount,
            attempt => TimeSpan.FromMilliseconds(cfg.RetryBackoffMilliseconds[attempt - 1]),
            onRetry: (outcome, timespan, retryNumber, ctx) =>
            {
                var metrics = provider.GetRequiredService<MetricsStore>();
                metrics.RecordRetry();
                retriesCounter.Inc();
                provider.GetRequiredService<ILogger<Program>>()
                    .LogWarning("Retry {RetryNumber} after {Delay}ms: {Reason}",
                        retryNumber, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
            });

    // NOTE: make retry the outer policy and the circuit breaker inner so the breaker counts each attempt.
    return Policy.WrapAsync(retry, circuitBreaker);
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