using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using src.Components;
using src.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONFIGURACIÓN (JSON + User Secrets + Env Vars)
// Orden de precedencia (último gana):
//   appsettings.json < appsettings.{Env}.json < User Secrets < Env Vars
// ============================================================

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
                 optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Configuration.AddEnvironmentVariables();

// Validación temprana: falla rápido si falta la cadena de conexión
var connStringPreview = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connStringPreview))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. " +
        "En desarrollo usa 'dotnet user-secrets set'. " +
        "En producción configura la variable de entorno 'ConnectionStrings__DefaultConnection'.");
}


// ============================================================
// RAZOR COMPONENTS
// ============================================================

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================
// AUTENTICACIÓN
// ============================================================

builder.Services
    .AddAuthentication("FinEduCookie")
    .AddCookie("FinEduCookie", options =>
    {
        options.Cookie.Name = "FinEdu.Auth";

        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

// ============================================================
// SERVICIOS
// ============================================================

builder.Services.AddHttpClient();

builder.Services.AddScoped<MefService>();
builder.Services.AddScoped<NlqService>();
builder.Services.AddScoped<OeceService>();
builder.Services.AddScoped<BackendService>();
builder.Services.AddScoped<OeceBackendService>();

var app = builder.Build();

// ============================================================
// PIPELINE
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// LOGIN
// ============================================================

app.MapPost("/account/login", async (
    HttpContext httpContext,
    IConfiguration configuration) =>
{
    var form = await httpContext.Request.ReadFormAsync();

    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=1");
    }

    var connectionString =
        configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem(
            "No se encontró ConnectionStrings:DefaultConnection");
    }

    await using var connection =
        new NpgsqlConnection(connectionString);

    await connection.OpenAsync();

    await using var command =
        new NpgsqlCommand(
            "SELECT * FROM authenticate_app_user($1, $2)",
            connection);

    command.Parameters.AddWithValue(username);
    command.Parameters.AddWithValue(password);

    await using var reader =
        await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return Results.Redirect("/login?error=1");
    }

    var userId = reader.GetInt32(0);
    var dbUsername = reader.GetString(1);
    var fullName = reader.GetString(2);
    var roleCode = reader.GetString(3);
    var roleName = reader.GetString(4);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Name, dbUsername),
        new Claim("FullName", fullName),
        new Claim(ClaimTypes.Role, roleCode),
        new Claim("RoleName", roleName)
    };

    var identity = new ClaimsIdentity(
        claims,
        "FinEduCookie");

    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        "FinEduCookie",
        principal);

    return Results.Redirect("/");
});

// ============================================================
// LOGOUT
// ============================================================

app.MapPost("/account/logout", async (
    HttpContext httpContext) =>
{
    await httpContext.SignOutAsync("FinEduCookie");

    return Results.Redirect("/login");
});

// ============================================================
// BLAZOR
// ============================================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();