using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

var rnd = new Random();

app.MapGet("/produce", () =>
{
    int errorChance = 80;
    // 80% chance to fail
    if (rnd.Next(100) < errorChance)
    {
        return Results.Problem(statusCode: 500, detail: "Transient error occurred");
    }

    return Results.Ok($"Produced at {DateTime.UtcNow}");
});

app.Run();