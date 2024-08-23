using System.Security.Claims;
using DataLibrary.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketplaceApi.Authorization;

public class SubscriptionStatusAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly UserManager<User> _userManager;

    public SubscriptionStatusAuthorizationFilter(UserManager<User> userManager)
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

        var roles = context.HttpContext.User.FindAll(ClaimTypes.Role);
        if (roles.Any(r => r.Value == nameof(Role.Subscriber)))
        {
            if (!await IsSubscriber(user))
            {
                context.Result = new UnauthorizedResult();
            }
            return;
        }

        if (await IsSubscriber(user))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    private async Task<bool> IsSubscriber(User user)
    {
        return await _userManager.IsInRoleAsync(user, nameof(Role.Subscriber));
    }
}