using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using ProducerApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the local store
builder.Services.AddSingleton<ErrorChanceStore>();

// Register and start SignalR client to ConfigApi
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var store = sp.GetRequiredService<ErrorChanceStore>();
    var hubUrl = config["ConfigApi:HubUrl"]!;

    var connection = new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect()
        .Build();

    // Subscribe to broadcasts
    connection.On<double>(
        "BroadcastErrorChance",
        newChance =>
        {
            store.SetChance(newChance);
            Console.WriteLine($"[SignalR] Updated errorChance to {newChance:P0}");
        });

    // Start connection
    _ = connection.StartAsync();

    return connection;
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
        return Results.Problem(statusCode: 500, detail: "Transient error occurred");

    return Results.Ok($"Produced at {DateTime.UtcNow}");

});

await app.RunAsync();