using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using ProducerApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the local store
builder.Services.AddSingleton<ErrorChanceStore>();

// Register and start SignalR client to ConfigApi
builder.Services.AddSingleton<HubConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var store = sp.GetRequiredService<ErrorChanceStore>();
    var hubUrl = config["ConfigApi:HubUrl"]!;

    var connection = new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect()
        .Build();

    // Subscribe to broadcasts
    connection.On<double>("BroadcastErrorChance", (newChance) =>
        {
            Console.WriteLine("---------------------->[ProducerApi] [ConfigApi] Hub connection started.");
            store.SetChance(newChance);
            Console.WriteLine($"[ProducerApi] [ConfigApi] Updated errorChance to {newChance:P0}");
        });

    // Start connection
    //_ = connection.StartAsync();
    if (connection is not null &&
            connection.State == HubConnectionState.Disconnected)
    {
        try
        {
            connection.StartAsync();
            Console.WriteLine("---------------------->[ProducerApi] [ConfigApi] Hub connection started.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"---------------------->[ProducerApi] [ConfigApi] Error *** starting hub connection: {ex.Message}");
        }
    }

    return connection!;
});


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

var rnd = new Random();

app.MapGet("/produce", (ErrorChanceStore store) =>
{
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