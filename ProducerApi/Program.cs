using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using ProducerApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the local store
builder.Services.AddSingleton<ErrorChanceStore>();

// Register and start SignalR client to ConfigApi
builder.Services.AddSingleton<CustomHubAdapter>();
//builder.Services.AddSingleton<HubConnection>(sp =>
//{
//    var config = sp.GetRequiredService<IConfiguration>();
//    var store = sp.GetRequiredService<ErrorChanceStore>();
//    var hubUrl = config["ConfigApi:HubUrl"]!;

//    Console.WriteLine($"---------------------->[ProducerApi] [ConfigApi] will try building a connection to {hubUrl}");
//    var connection = new HubConnectionBuilder()
//        .WithUrl(hubUrl)
//        .WithAutomaticReconnect()
//        .Build();

//    // Subscribe to broadcasts
//    connection.On<double>("BroadcastErrorChance", (newChance) =>
//    {
//        store.SetChance(newChance);
//        Console.WriteLine($"---------------------->[ProducerApi] [ConfigApi] Updated errorChance to {newChance:P0}");
//    });

//    // Start connection synchronously since we're in a constructor
//    try
//    {
//        connection.StartAsync().GetAwaiter().GetResult();
//        Console.WriteLine("---------------------->[ProducerApi] [ConfigApi] Hub connection started.");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"---------------------->[ProducerApi] [ConfigApi] Error *** starting hub connection: {ex.Message}");
//    }

//    return connection;
//});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.MapPut("/start", (CustomHubAdapter hubAdapter, [FromBody] string input) =>
{
    Console.WriteLine($"---------------------->[ProducerApi] Start instruction received. Input:{input}");
    var status = hubAdapter.GetConnectionStatus();
    if (status == "Disconnected" || status == "Not initialized")
    {
        Console.WriteLine($"---------------------->[ProducerApi] Starting hub connection to ConfigApi...");
        hubAdapter.StartHubConnectionAsync().GetAwaiter().GetResult();
    }
    status = hubAdapter.GetConnectionStatus();
    return $"Producer online, with hub connection status: {status}";
});
app.MapGet("/produce", (ErrorChanceStore store) =>
{
    Console.WriteLine($"---------------------->[ProducerApi] Produce instruction received.");
    var rnd = new Random();
    var chance = store.GetChance();

    if (rnd.NextDouble() < chance)
    {
        Console.WriteLine($"---------------------->[Produce] Returning Error !!! (chance {chance:P0})");
        return Results.Problem(statusCode: 500, detail: "Transient error occurred");
    }
    Console.WriteLine($"---------------------->[Produce] Returning Success (chance {chance:P0})");
    return Results.Ok($"Produced at {DateTime.UtcNow}");

});

await app.RunAsync();