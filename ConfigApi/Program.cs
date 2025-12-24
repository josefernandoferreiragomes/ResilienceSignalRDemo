using ConfigApi;

var builder = WebApplication.CreateBuilder(args);

// Register in-memory store and SignalR
builder.Services.AddSingleton<ErrorChanceStore>();
builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<ConfigHub>("/confighub");

app.MapGet("/health", () =>
{
    Console.WriteLine("---------------------->[ConfigApi] Health check received.");
    return "ConfigApi is running.";
});

app.MapGet("/errorchance", (ErrorChanceStore store) =>
{
    var config = store.GetChance();
    Console.WriteLine($"---------------------->[ConfigApi] Error chance requested config: {config.ToString()}.");
    return config;
});

await app.RunAsync();

