using ConsumerApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register services

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<MetricsStore>();
var initialConfig = builder.Configuration
    .GetSection("ResilienceConfig")
    .Get<ResilienceConfig>() ?? new ResilienceConfig();
builder.Services.AddSingleton(new ResilienceConfigStore(initialConfig));

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
                metrics.SetLastReset(DateTime.UtcNow);
                provider.GetRequiredService<ILogger<Program>>()
                    .LogWarning("Circuit opened for {Delay}s: {Reason}",
                        breakDelay.TotalSeconds, outcome.Exception?.Message);
            },
            onReset: () =>
            {
                var metrics = provider.GetRequiredService<MetricsStore>();
                metrics.SetCircuitState("Closed");
                provider.GetRequiredService<ILogger<Program>>()
                    .LogWarning("Circuit closed");
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
                provider.GetRequiredService<ILogger<Program>>()
                    .LogWarning("Retry {RetryNumber} after {Delay}ms: {Reason}",
                        retryNumber, timespan.TotalMilliseconds, outcome.Exception?.Message);
            });

    // Combine policies - circuit breaker first, then retry
    return Policy.WrapAsync(retry, circuitBreaker);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// Endpoints
app.MapGet("/consume", async (IHttpClientFactory factory, MetricsStore metrics) =>
{
    Console.WriteLine($"---------------------->[ConsumerApi] /consume called.");
    metrics.RecordCall();
    var client = factory.CreateClient("ProducerClient");
    var response = await client.GetAsync("/produce");

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(
            statusCode: (int)response.StatusCode,
            detail: "Producer error");
    }

    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"---------------------->[ConsumerApi] Received from Producer: {content}");
    return Results.Ok(content);
});

app.MapGet("/metrics", (MetricsStore metrics) =>
{
    Console.WriteLine($"---------------------->[ConsumerApi] Metrics requested: {metrics.ToString()}.");
    return Results.Ok(metrics);
});

app.MapGet("/config", (ResilienceConfigStore store) =>
{
    var config = store.GetConfig();
    Console.WriteLine($"---------------------->[ConsumerApi] Config requested: {config.ToString()}.");
    return config;
});

app.MapPut("/config", (ResilienceConfig newCfg, ResilienceConfigStore store) =>
{
    Console.WriteLine($"---------------------->[ConsumerApi] Config update received: {newCfg.ToString()}.");
    store.UpdateConfig(newCfg);
    return Results.NoContent();
});

await app.RunAsync();