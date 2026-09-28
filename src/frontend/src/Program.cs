using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using DotNetEnv;
using src.Components;
using src.Interfaces;
using src.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONFIGURACIÓN - Cargar .env file y variables de entorno
// ============================================================
// Cargar archivo .env desde la raíz del proyecto frontend
var envPath = Path.Combine(builder.Environment.ContentRootPath, "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

// Agregar variables de entorno del sistema
builder.Configuration.AddEnvironmentVariables();

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
builder.Services.AddScoped<IBackendService, BackendService>();

// Password hashing service
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

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
// HELPER: Construir connection string desde variables de entorno
// ============================================================

string BuildConnectionString(IConfiguration configuration)
{
    // Primero intentar DATABASE_URL completa (para Render/Producción)
    var databaseUrl = configuration["DATABASE_URL"] 
        ?? configuration["ConnectionStrings:DATABASE_URL"]
        ?? Environment.GetEnvironmentVariable("DATABASE_URL");
    
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        return databaseUrl;
    }

    // Si no hay DATABASE_URL, construir desde variables individuales
    var host = configuration["DB_HOST"] ?? Environment.GetEnvironmentVariable("DB_HOST");
    var port = configuration["DB_PORT"] ?? Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var database = configuration["DB_NAME"] ?? Environment.GetEnvironmentVariable("DB_NAME");
    var username = configuration["DB_USER"] ?? Environment.GetEnvironmentVariable("DB_USER");
    var password = configuration["DB_PASSWORD"] ?? Environment.GetEnvironmentVariable("DB_PASSWORD");

    if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return null; // Faltan variables requeridas
    }

    // Validar que el puerto sea numérico
    if (!int.TryParse(port, out _))
    {
        port = "5432"; // Fallback
    }

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = int.Parse(port),
        Database = database,
        Username = username,
        Password = password,
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    };

    return builder.ConnectionString;
}

// ============================================================
// LOGIN - Usa hash de contraseña (BCrypt)
// ============================================================

app.MapPost("/account/login", async (
    HttpContext httpContext,
    IConfiguration configuration,
    IPasswordHasher passwordHasher) =>
{
    var form = await httpContext.Request.ReadFormAsync();

    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=1");
    }

    var connectionString = BuildConnectionString(configuration);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem(
            "No se encontró configuración de base de datos. Verifique variables de entorno: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD o DATABASE_URL");
    }

    await using var connection =
        new NpgsqlConnection(connectionString);

    await connection.OpenAsync();

    // Primero obtener el usuario por username (sin contraseña) - join con app_roles
    await using var command =
        new NpgsqlCommand(
            @"SELECT u.id, u.username, u.full_name, r.code as role_code, r.name as role_name, u.password_hash 
              FROM app_users u
              LEFT JOIN app_roles r ON u.role_id = r.id
              WHERE u.username = $1",
            connection);

    command.Parameters.AddWithValue(username);

    await using var reader =
        await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        // Usuario no encontrado - usar tiempo constante para evitar timing attacks
        return Results.Redirect("/login?error=1");
    }

    var userId = reader.GetInt32(0);
    var dbUsername = reader.GetString(1);
    var fullName = reader.GetString(2);
    var roleCode = reader.IsDBNull(3) ? "USER" : reader.GetString(3);
    var roleName = reader.IsDBNull(4) ? "Usuario" : reader.GetString(4);
    var passwordHash = reader.GetString(5);

    // Verificar contraseña con BCrypt
    if (!passwordHasher.VerifyPassword(password, passwordHash))
    {
        // Contraseña incorrecta - usar tiempo constante para evitar timing attacks
        return Results.Redirect("/login?error=1");
    }

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