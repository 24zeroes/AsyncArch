using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Popug.Contracts;
using Popug.Infrastructure.Security;

namespace Popug.Infrastructure.Security;

public class AuthNMiddleware
{
    private readonly RequestDelegate next;
  
    public AuthNMiddleware(RequestDelegate next)
    {
        this.next = next;
    }
  
    public async Task InvokeAsync(HttpContext context)
    {
        var cryptor = context.RequestServices.GetRequiredService<Cryptor>();
        
        // Attempt to retrieve the custom security cookie.
        var token = context.Request.Cookies["ssoToken"];
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Decrypt and validate the cookie.
                // You would implement this to match your crypto scheme.
                // For example, it might decrypt the value and then verify an HMAC signature.
                var userData = JsonSerializer.Deserialize<SecurityToken>(cryptor.Decrypt(token));
                
                if (userData != null)
                {
                    // Build the claims. You might include claims like name, roles, etc.
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userData.UserName),
                        new Claim(ClaimTypes.NameIdentifier, userData.UserId.ToString())
                        // Add additional claims as needed.
                    };

                    // Optionally add role claims.
                    foreach (var role in userData.Claims)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    // Create an identity and principal.
                    var identity = new ClaimsIdentity(claims, "ssoToken");
                    var principal = new ClaimsPrincipal(identity);

                    // Set the principal on the HttpContext.
                    context.User = principal;
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions (e.g., decryption failures) as needed.
                // If decryption fails, it's best to treat the request as unauthenticated.
            }
        }

        // Call the next delegate/middleware in the pipeline.
        await next(context);
    }
}