namespace src.Security;

// Códigos de rol guardados en la BD y emitidos en el claim ClaimTypes.Role al iniciar sesión.
public static class AuthRoles
{
    public const string Admin = "ADMIN";
    public const string User = "USER";
}

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";
}
