
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
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
    .SetApplicationName("FinEduBot");

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

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

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

    var loginConnectionString =
        configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(loginConnectionString))
    {
        return Results.Problem(
            "No se encontró ConnectionStrings:DefaultConnection");
    }

    await using var connection =
        new NpgsqlConnection(loginConnectionString);

    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        "SELECT * FROM authenticate_app_user($1, $2)",
        connection);

    command.Parameters.AddWithValue(username);
    command.Parameters.AddWithValue(password);

    await using var reader = await command.ExecuteReaderAsync();

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
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Name, dbUsername),
        new("FullName", fullName),
        new(ClaimTypes.Role, roleCode),
        new("RoleName", roleName)
    };

    var identity = new ClaimsIdentity(claims, "FinEduCookie");
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync("FinEduCookie", principal);

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
