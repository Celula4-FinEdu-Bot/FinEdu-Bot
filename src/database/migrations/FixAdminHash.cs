// Update admin password hash to "Admin123*"

using System;
using System.IO;
using Npgsql;
using BCryptNet = BCrypt.Net;

var connectionString = File.Exists("connection.txt")
    ? File.ReadAllText("connection.txt").Trim()
    : "Host=dpg-dahpo6ss728c73d2jhpg-a.oregon-postgres.render.com;Port=5432;Database=caso5_db;Username=finedu_db;Password=YFgZWaPOBYUpqYehqLTGfjIE0UuEAt1c;SSL Mode=Require;Trust Server Certificate=true";

try
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    Console.WriteLine("✓ Conexión exitosa");

    // Generar hash correcto para "Admin123*"
    var correctHash = BCryptNet.BCrypt.EnhancedHashPassword("Admin123*", BCryptNet.HashType.SHA384, 12);
    Console.WriteLine($"Hash correcto para 'Admin123*': {correctHash}");

    // Verificar que el hash funciona
    var verify = BCryptNet.BCrypt.EnhancedVerify("Admin123*", correctHash, BCryptNet.HashType.SHA384);
    Console.WriteLine($"Verificación: {verify}");

    // Actualizar en BD
    await using var cmd = new NpgsqlCommand(
        "UPDATE app_users SET password_hash = @hash WHERE username = 'admin';", 
        conn);
    cmd.Parameters.AddWithValue("hash", correctHash);
    
    var rows = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Filas actualizadas: {rows}");

    // Verificar en BD
    await using var checkCmd = new NpgsqlCommand(
        "SELECT password_hash FROM app_users WHERE username = 'admin';", 
        conn);
    var newHash = await checkCmd.ExecuteScalarAsync();
    Console.WriteLine($"Nuevo hash en BD: {newHash}");

    // Test final
    var finalVerify = BCryptNet.BCrypt.EnhancedVerify("Admin123*", (string)newHash, BCryptNet.HashType.SHA384);
    Console.WriteLine($"Test final login: {finalVerify}");

    Console.WriteLine("\n=== ¡Actualizado! ===");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
}