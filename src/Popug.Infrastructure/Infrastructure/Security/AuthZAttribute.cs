namespace Popug.Infrastructure.Security;

public class AuthZAttribute : Attribute
{
    public string[] AllowedScopes { get; }

    public AuthZAttribute(params string[] allowedScopes)
    {
        AllowedScopes = allowedScopes;
    }
}