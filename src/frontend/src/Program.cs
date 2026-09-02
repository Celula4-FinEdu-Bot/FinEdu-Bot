using src.Components;
using src.Services;
using src.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Razor Components + Interactive Server
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();


// Servicios de la aplicación
builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddScoped<MefService>();
builder.Services.AddScoped<NlqService>();


var app = builder.Build();


// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();
app.MapControllers();


// Blazor Web App
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
