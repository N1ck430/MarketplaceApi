using DataLibrary.Models.User;
using MarketplaceApi.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SubscriptionController : ControllerBase
{
    private readonly ILogger<SubscriptionController> _logger;
    private readonly IUserService _userService;

    public SubscriptionController(
        ILogger<SubscriptionController> logger,
        IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> AddSubscriptionToUser(string userId, int subscriptionTypeId)
    {
        try
        {
            return await _userService.AddSubscriptionToUser(userId, subscriptionTypeId);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "SubscriptionController.AddSubscriptionToUser | Could not add subscriptionType {subscriptionTypeId} to user {userId}",
                subscriptionTypeId, userId);
            return TypedResults.BadRequest();

        }
    }
}