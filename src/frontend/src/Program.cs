
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.AspNetCore.HttpOverrides;

using Npgsql;

using src.Components;
using src.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONFIGURACIÓN
//
// WebApplication.CreateBuilder carga appsettings, User Secrets
// en desarrollo y variables de entorno.
// En Render, las variables de entorno tienen prioridad sobre
// los valores de configuración de los archivos JSON.
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. " +
        "En desarrollo configura User Secrets. " +
        "En Render configura ConnectionStrings__DefaultConnection.");
}

// ============================================================
// DATA PROTECTION
//
// No se configura una ruta de claves ni un certificado.
// ASP.NET Core utilizará su almacenamiento predeterminado.
//
// En el plan gratuito de Render, las claves pueden perderse
// cuando se destruye el contenedor. Esto puede cerrar sesiones
// existentes después de un reinicio o redespliegue.
// ============================================================

builder.Services
    .AddDataProtection()
    .SetApplicationName("FinEduBot")
    .UseEphemeralDataProtectionProvider();

// ============================================================
// PROXY INVERSO DE RENDER
//
// Render termina HTTPS y reenvía la petición al contenedor.
// Procesar X-Forwarded-Proto antes de HTTPS redirection evita
// que ASP.NET Core interprete erróneamente la petición como HTTP.
//
// La limpieza de proxies conocidos presupone que el servicio
// solo recibe tráfico web a través del proxy de Render.
// ============================================================

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.ForwardLimit = 1;

    if (!builder.Environment.IsDevelopment())
    {
        // Render puede usar direcciones variables para sus proxies.
        // Solo usar este ajuste si el contenedor está protegido
        // detrás del proxy de Render y no expuesto directamente.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

// ============================================================
// RAZOR COMPONENTS / BLAZOR
// ============================================================

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================
// AUTENTICACIÓN POR COOKIES
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

        // En producción, la cookie solo se envía mediante HTTPS.
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
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


var dataProtectionPath =
    Environment.GetEnvironmentVariable("DATA_PROTECTION_PATH");

if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);

    builder.Services
        .AddDataProtection()
        .SetApplicationName("FinEduBot")
        .PersistKeysToFileSystem(
            new DirectoryInfo(dataProtectionPath));
}

var app = builder.Build();

// ============================================================
// PIPELINE HTTP
// ============================================================

// Debe ejecutarse antes de HSTS y de HTTPS redirection.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// ============================================================
// LOGIN
// ============================================================

app.MapPost("/account/login", async (
    HttpContext httpContext,
    IConfiguration configuration,
    ILogger<Program> logger) =>
{
    var form = await httpContext.Request.ReadFormAsync();

    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=1");
    }

    try
    {
        var loginConnectionString =
            configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(loginConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT * FROM authenticate_app_user($1, $2)", connection);
        command.Parameters.AddWithValue(username);
        command.Parameters.AddWithValue(password);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            logger.LogWarning("Login fallido: sin filas para el usuario {User}", username);
            return Results.Redirect("/login?error=1");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, reader.GetInt32(0).ToString()),
            new(ClaimTypes.Name, reader.GetString(1)),
            new("FullName", reader.GetString(2)),
            new(ClaimTypes.Role, reader.GetString(3)),
            new("RoleName", reader.GetString(4))
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "FinEduCookie"));
        await httpContext.SignInAsync("FinEduCookie", principal);

        logger.LogInformation("Login correcto: {User}", username);
        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error en login para {User}", username);
        return Results.Redirect("/login?error=2");
    }
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
