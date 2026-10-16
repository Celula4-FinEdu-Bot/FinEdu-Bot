using BCryptNet = BCrypt.Net;
using src.Interfaces;

namespace src.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

        // BCrypt generates a salt automatically and includes it in the hash
        // Work factor 12 is a good balance between security and performance
        // Using SHA384 pre-hashing for increased entropy
        return BCryptNet.BCrypt.EnhancedHashPassword(password, BCryptNet.HashType.SHA384, 12);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCryptNet.BCrypt.EnhancedVerify(password, hashedPassword, BCryptNet.HashType.SHA384);
        }
        catch
        {
            // If verification fails for any reason (invalid hash format, etc.)
            return false;
        }
    }
}