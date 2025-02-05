using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace Popug.Infrastructure.Security;
public class AuthZFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Access the current user principal
        var user = context.HttpContext.User;
        
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null)
            return;
        
        var authZAttribute = endpoint.Metadata.GetMetadata<AuthZAttribute>();
        if (authZAttribute == null) 
            return;
        
        // Example: Asynchronously check if the user is authenticated
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (authZAttribute.AllowedScopes.All(scope => !context.HttpContext.User.IsInRole(scope)))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}