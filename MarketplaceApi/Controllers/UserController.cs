using System.Text.Json;
using DataLibrary.Models.Requests.General;
using DataLibrary.Models.Requests.User;
using DataLibrary.Models.Responses.General;
using DataLibrary.Models.Responses.User;
using DataLibrary.Models.User;
using MarketplaceApi.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService,
        ILoggerFactory loggerFactory)
    {
        _userService = userService;
        _logger = loggerFactory.CreateLogger<UserController>();
    }

    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> Register(RegisterRequest register)
    {
        try
        {
            var response = await _userService.Register(register);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.Register | Could not register User {UserName} {Email}",
                register.Username, register.Email);
            return TypedResults.BadRequest();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> Login(LoginRequest login)
    {
        try
        {
            var response = await _userService.Login(login);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserConroller.Login | Could not login User {UserName}", login.UserName);
            return TypedResults.BadRequest();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IResult> Refresh(string refreshToken)
    {
        try
        {
            var response = await _userService.Refresh(refreshToken);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.Refresh | Could not get refresh-token");
            return TypedResults.BadRequest();
        }
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(ExtendedInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IResult> CurrentUserInfo()
    {
        try
        {
            var response = await _userService.GetCurrentUserInfo();
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.CurrentUserInfo | Could not get currentUserInfo");
            return TypedResults.BadRequest();
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpGet("{userSequenceId:int}")]
    [ProducesResponseType(typeof(ExtendedInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IResult> ExtendedUserInfo(int userSequenceId)
    {
        try
        {
            var response = await _userService.GetExtendedUserInfo(userSequenceId);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "UserController.ExtendedUserInfo | Could not get UserInfo for userSequenceId {userSequenceId}",
                userSequenceId);
            return TypedResults.BadRequest();
        }
    }

    [Authorize]
    [HttpGet("{userSequenceId:int}")]
    [ProducesResponseType(typeof(InfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IResult> UserInfo(int userSequenceId)
    {
        try
        {
            var response = await _userService.GetUserInfo(userSequenceId);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.UserInfo |  Could not get UserInfo for userSequenceId {userSequenceId}",
                userSequenceId);
            return TypedResults.BadRequest();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IResult> ConfirmEmail(ConfirmEmailRequest confirmEmail)
    {
        try
        {
            var result = await _userService.ConfirmEmail(confirmEmail);
            return result;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.ConfirmEmail | Could not confirm Email for User {UserId}",
                confirmEmail.UserId);
            return TypedResults.BadRequest();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> ResetPassword(ResetPasswordRequest resetPassword)
    {
        try
        {
            var result = await _userService.ResetPassword(resetPassword);
            return result;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.ResetPassword | Could not reset Password for {UserId} {Email}",
                resetPassword.UserId, resetPassword.Email);
            return TypedResults.BadRequest();
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(ListResponse<InfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseSearchRequest), StatusCodes.Status204NoContent)]
    public IResult SearchUsers(UserSearchRequest searchUsers)
    {
        try
        {
            var result = _userService.SearchUsers(searchUsers);
            return result;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.SearchUsers | Error Searching Users {SearchRequest}",
                JsonSerializer.Serialize(searchUsers));
            return TypedResults.BadRequest();
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> LockOutUser(string userId)
    {
        try
        {
            var result = await _userService.LockOutUser(userId);
            return result;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.LockOutUser | Could not lockout user {UserId}", userId);
            return TypedResults.BadRequest();
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateUserRoles(string userId, List<Role> roles)
    {
        try
        {
            var result = await _userService.UpdateUserRoles(userId, roles);
            return result;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserController.UpdateUserRoles | Could not update user roles for {UserId}", userId);
            return TypedResults.BadRequest();
        }
    }
}