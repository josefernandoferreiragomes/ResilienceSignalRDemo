using ManagementDashboard.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Leave Blazorise integration out for now; keep UI lightweight

builder.Services.AddHttpClient("ConsumerApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ConsumerApi:BaseUrl"]!);
});

builder.Services.AddHttpClient("ProducerApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProducerApi:BaseUrl"]!);
});

builder.Services.AddHttpClient("ConfigApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ConfigApi:HubUrl"]!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        ctx.Context.Response.Headers["Pragma"] = "no-cache";
        ctx.Context.Response.Headers["Expires"] = "0";
    }
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
