using FinEduBot.Frontend.Components;
using FinEduBot.Frontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<MefService>();

builder.Services.AddHttpClient<MefService>(client =>
{
    client.BaseAddress = new Uri("https://api.datosabiertos.mef.gob.pe/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});



builder.Services.AddScoped<MefService>();
builder.Services.AddScoped<NlqService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();