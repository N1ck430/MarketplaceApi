using DataLibrary.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketplaceApi.Authorization;

public class LockedInAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly UserManager<User> _userManager;

    public LockedInAuthorizationFilter(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var hasAuthorizeAttribute = context.ActionDescriptor.EndpointMetadata.Any(em =>
            em.GetType() == typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute));

        if (!hasAuthorizeAttribute)
        {
            return;
        }

        if (await _userManager.GetUserAsync(context.HttpContext.User) is not { } user)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}