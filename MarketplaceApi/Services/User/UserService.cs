using DataLibrary.Models.Application;
using DataLibrary.Models.Requests.User;
using DataLibrary.Models.Responses.General;
using DataLibrary.Models.Responses.User;
using DataLibrary.Models.User;
using DataLibrary.Services.DateTime;
using DataLibrary.Services.User;
using MarketplaceApi.Helpers;
using MarketplaceApi.Services.Cache;
using MarketplaceApi.Services.Mail;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneOf;
using SkiaSharp;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DataLibrary.Models.Software;
using DataLibrary.Services.Software;
using AccessTokenResponse = DataLibrary.Models.Responses.User.AccessTokenResponse;

namespace MarketplaceApi.Services.User;

public class UserService : IUserService
{
    private readonly SignInManager<DataLibrary.Models.User.User> _signInManager;
    private readonly UserManager<DataLibrary.Models.User.User> _userManager;
    private readonly IUserEmailStore<DataLibrary.Models.User.User> _emailStore;
    private readonly TimeProvider _timeProvider;
    private readonly IUserDataService _userDataService;
    private readonly ISecureDataFormat<AuthenticationTicket> _refreshTokenProtector;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationSettings _appSettings;
    private readonly IMailService _mailService;
    private readonly ILogger<UserService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISoftwareDataService _softwareDataService;

    public UserService(
        SignInManager<DataLibrary.Models.User.User> signInManager,
        UserManager<DataLibrary.Models.User.User> userManager,
        IUserStore<DataLibrary.Models.User.User> userStore,
        TimeProvider timeProvider,
        IUserDataService userDataService,
        IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ApplicationSettings> options,
        IMailService mailService,
        ILoggerFactory loggerFactory,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider,
        ISoftwareDataService softwareDataService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _timeProvider = timeProvider;
        _userDataService = userDataService;
        _httpContextAccessor = httpContextAccessor;
        _mailService = mailService;
        _cacheService = cacheService;
        _dateTimeProvider = dateTimeProvider;
        _softwareDataService = softwareDataService;
        _refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        _emailStore = (IUserEmailStore<DataLibrary.Models.User.User>)userStore;
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        _appSettings = options.Value;
        _logger = loggerFactory.CreateLogger<UserService>();
    }

    public async Task<Results<Ok, ValidationProblem>> Register(RegisterRequest register)
    {
        var user = new DataLibrary.Models.User.User();
        await _userManager.SetUserNameAsync(user, register.Username);
        await _emailStore.SetEmailAsync(user, register.Email, CancellationToken.None);
        user.RegisterDate = user.LastLoginDate = _timeProvider.GetUtcNow().DateTime;
        user.UserSequenceId = await _userDataService.GetHighestUserSequence() + 1;
        user.ProfilePicture = GenerateProfilePicture(register.Username);

        var result = await _userManager.CreateAsync(user, register.Password);

        if (!result.Succeeded)
        {
            return CreateValidationProblem(result);
        }

        await _userManager.AddToRoleAsync(user, Role.User.ToString());
        await SendConfirmationEmail(user, register.Email);

        return TypedResults.Ok();
    }

    public async Task<Results<Ok<AccessTokenResponse>, ProblemHttpResult, IResult>> Login(LoginRequest login)
    {
        var result = await _signInManager.PasswordSignInAsync(login.UserName, login.Password, true, true);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(login.UserName);
            user!.LastLoginDate = _timeProvider.GetUtcNow().DateTime;
            await _userManager.UpdateAsync(user);
            if (!await _userManager.IsInRoleAsync(user, Role.User.ToString()))
            {
                await _userManager.AddToRoleAsync(user, Role.User.ToString());
            }

            return TypedResults.Empty;
        }

        if (await _userManager.FindByNameAsync(login.UserName) is not { EmailConfirmed: false } otherUser)
        {
            return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
        }

        await SendConfirmationEmail(otherUser, otherUser.Email!);
        return TypedResults.Problem("Please confirm your email. A confirmation email was send.", statusCode: StatusCodes.Status400BadRequest);

    }

    public async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
        Refresh(string refreshToken)
    {
        var refreshTicket = _refreshTokenProtector.Unprotect(refreshToken);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc ||
            _timeProvider.GetUtcNow() >= expiresUtc ||
            await _signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not { } user)
        {
            return TypedResults.Challenge();
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return TypedResults.Unauthorized();
        }

        var newPrincipal = await _signInManager.CreateUserPrincipalAsync(user);
        return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    public async Task<Results<Ok, ProblemHttpResult>> ConfirmEmail(ConfirmEmailRequest confirmEmail)
    {
        if (await _userManager.FindByIdAsync(confirmEmail.UserId) is not { } user)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }

        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(confirmEmail.Code));

        var result = await _userManager.ConfirmEmailAsync(user, code);

        if (!result.Succeeded)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }

        return TypedResults.Ok();
    }

    public async Task<Results<Ok<ExtendedInfoResponse>, ProblemHttpResult>> GetCurrentUserInfo()
    {
        var result = await GetCurrentUser();

        if (result.IsT0)
        {
            return await MakeExtendedInfoResponse(result.AsT0);
        }

        return result.AsT1;
    }

    public async Task<Results<Ok<ExtendedInfoResponse>, ProblemHttpResult>> GetExtendedUserInfo(int userSequenceId)
    {
        var result = await GetUser(userSequenceId, true);
        if (result.IsT0)
        {
            return await MakeExtendedInfoResponse(result.AsT0);
        }

        return result.AsT1;
    }

    public async Task<Results<Ok<InfoResponse>, ProblemHttpResult>> GetUserInfo(int userSequenceId)
    {
        var result = await GetUser(userSequenceId, true);
        if (result.IsT0)
        {
            return await MakeInfoResponse(result.AsT0);
        }

        return result.AsT1;
    }

    public async Task<DataLibrary.Models.User.User?> GetUser(string userId, bool fromCache)
    {
        return fromCache
            ? await _cacheService.GetAndSetCacheEntry(CacheKeys.MakeCacheKey(CacheKeys.User, userId),
                GetUserFromManager)
            : await GetUserFromManager();

        async Task<DataLibrary.Models.User.User?> GetUserFromManager()
        {
            return await _userManager.Users
                .Include(u => u.Subscriptions)
                .ThenInclude(s => s.SubscriptionType)
                .ThenInclude(st => st.Software)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }

    public async Task<Results<Ok, ValidationProblem>> ResetPassword(ResetPasswordRequest resetRequest)
    {
        if (!string.IsNullOrEmpty(resetRequest.ResetCode) && string.IsNullOrEmpty(resetRequest.Password))
        {
            return CreateValidationProblem("MissingNewPassword", "A password reset code was provided without a new password.");
        }

        var user = string.IsNullOrEmpty(resetRequest.Email)
            ? string.IsNullOrEmpty(resetRequest.UserId) ? null : await _userManager.FindByIdAsync(resetRequest.UserId)
            : await _userManager.FindByEmailAsync(resetRequest.Email);

        if (user is null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            if (!string.IsNullOrEmpty(resetRequest.ResetCode))
            {
                return CreateValidationProblem(IdentityResult.Failed(_userManager.ErrorDescriber.InvalidToken()));
            }
        }
        else if (string.IsNullOrEmpty(resetRequest.ResetCode))
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var url = $"{_appSettings.ClientPaths.BaseUrl}/{_appSettings.ClientPaths.ResetPassword}/{user.Id}/{code}";

            var mailSettings = new MailSettings
            {
                Subject = "Reset your password",
                TemplateName = "ResetPassword",
                TemplateData = new
                {
                    ResetPasswordLink = url
                }
            };

            await _mailService.SendMail(mailSettings, user.Email!);
        }
        else if (!string.IsNullOrEmpty(resetRequest.Password))
        {
            IdentityResult result;
            try
            {
                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetRequest.ResetCode));
                result = await _userManager.ResetPasswordAsync(user, code, resetRequest.Password);
            }
            catch
            {
                result = IdentityResult.Failed(_userManager.ErrorDescriber.InvalidToken());
            }

            if (!result.Succeeded)
            {
                return CreateValidationProblem(result);
            }
        }

        return TypedResults.Ok();
    }

    public Results<Ok<ListResponse<InfoResponse>>, ProblemHttpResult> SearchUsers(
        UserSearchRequest searchRequest)
    {
        try
        {
            IEnumerable<DataLibrary.Models.User.User> userSearch = searchRequest.UserSearchOrder switch
            {
                UserSearchOrder.Id => searchRequest.OrderDesc
                    ? _userManager.Users.AsNoTracking().OrderByDescending(u => u.UserSequenceId)
                    : _userManager.Users.AsNoTracking().OrderBy(u => u.UserSequenceId),
                UserSearchOrder.Name => searchRequest.OrderDesc
                    ? _userManager.Users.AsNoTracking().OrderByDescending(u => u.UserName)
                    : _userManager.Users.AsNoTracking().OrderBy(u => u.UserName),
                UserSearchOrder.RegisterDate => searchRequest.OrderDesc
                    ? _userManager.Users.AsNoTracking().OrderByDescending(u => u.RegisterDate)
                    : _userManager.Users.AsNoTracking().OrderBy(u => u.RegisterDate),
                _ => throw new ArgumentOutOfRangeException(nameof(searchRequest.UserSearchOrder))
            };

            if (!string.IsNullOrEmpty(searchRequest.SearchText))
            {
                userSearch = userSearch.Where(u =>
                    u.UserName!.Contains(searchRequest.SearchText, StringComparison.InvariantCultureIgnoreCase) ||
                    u.Email!.Contains(searchRequest.SearchText, StringComparison.InvariantCultureIgnoreCase));
            }

            var userList = userSearch.ToImmutableList();

            var response = new ListResponse<InfoResponse>
            {
                Count = userList.Count,
                ListEntries = userList.Skip(searchRequest.PageSize * searchRequest.Page)
                    .Take(searchRequest.PageSize + (searchRequest.PageSize * searchRequest.Page)).Select(async u => new InfoResponse
                    {
                        UserId = u.Id,
                        UserSequenceId = u.UserSequenceId,
                        Username = u.UserName!,
                        RegisterDate = u.RegisterDate,
                        Roles = await GetRolesForUser(u)
                    }).Select(u => u.Result)
            };


            return TypedResults.Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SearchUsers({searchRequest}) | Error while searching users",
                JsonSerializer.Serialize(searchRequest));
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<Results<Ok, ValidationProblem>> LockOutUser(string userId)
    {
        try
        {
            if (await _userManager.FindByIdAsync(userId) is not { } user)
            {
                return CreateValidationProblem("UserNotFound", "Could not find user");
            }

            var lockOutEnabledResult = await _userManager.SetLockoutEnabledAsync(user, true);

            if (!lockOutEnabledResult.Succeeded)
            {
                return CreateValidationProblem(lockOutEnabledResult);
            }

            var lockOutDateResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            if (!lockOutDateResult.Succeeded)
            {
                return CreateValidationProblem(lockOutDateResult);
            }

            return TypedResults.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "LockOutUser | Could not lock out user with id {userId}", userId);
            return CreateValidationProblem("LockOutError", "Could not lock out user.");
        }
    }

    public async Task<Results<Ok, ProblemHttpResult>> UpdateUserRoles(string userId, List<Role> roles)
    {
        var user = await GetUser(userId, false);

        if (user is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound);
        }

        foreach (var role in Enum.GetNames(typeof(Role)).Select(r => r.ToString()))
        {
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                continue;
            }

            var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, role);

            if (!removeRoleResult.Succeeded)
            {
                return LogUpdateRolesError(removeRoleResult);
            }

        }

        foreach (var role in roles.Select(r => r.ToString()))
        {
            if (await _userManager.IsInRoleAsync(user, role))
            {
                continue;
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role);

            if (!addRoleResult.Succeeded)
            {
                return LogUpdateRolesError(addRoleResult);
            }
        }

        RemoveUserFromCache(user);

        return TypedResults.Ok();

        ProblemHttpResult LogUpdateRolesError(IdentityResult updateRoleResult)
        {
            _logger.LogError("UpdateUserRoles | Could not update roles for User {UserId} {Errors}", userId,
                string.Join("\n", updateRoleResult.Errors.Select(e => $"{e.Code} | {e.Description}")));
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<Results<Ok, ProblemHttpResult>> AddSubscriptionToUser(string userId, int subscriptionTypeId)
    {
        var user = await GetUser(userId, false);
        if (user is null)
        {
            return TypedResults.Problem("User not found", statusCode: StatusCodes.Status404NotFound);
        }

        var subscriptionType = await _softwareDataService.GetSubscriptionType(subscriptionTypeId);
        if (subscriptionType is null)
        {
            return TypedResults.Problem("SubscriptionType not found", statusCode: StatusCodes.Status404NotFound);
        }

        user.Subscriptions.Add(MakeSubscription(subscriptionType, userId));

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await AddUserToRoleIfNecessary(user, Role.Subscriber);
            RemoveUserFromCache(user);
            return TypedResults.Ok();
        }

        return LogUpdateUserError(result, userId);
    }
    public void RemoveUserFromCache(DataLibrary.Models.User.User user)
    {
        _cacheService.RemoveCacheKey(CacheKeys.MakeCacheKey(CacheKeys.User, user.Id));
        _cacheService.RemoveCacheKey(CacheKeys.MakeCacheKey(CacheKeys.User, user.UserSequenceId.ToString()));
    }

    public byte[] GenerateProfilePicture(string userName)
    {
        const int imageSize = 500;
        var textColor = new SKColor(255, 255, 255, 150);
        var backgroundColor = GetColorFromUsername(userName);
        var firstLetter = userName[0].ToString().ToUpper();

        using var bitmap = new SKBitmap(imageSize, imageSize);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(backgroundColor);

        using var font = new SKFont(SKTypeface.FromFamilyName("Arial"), imageSize / 2f);
        using var blob = SKTextBlob.Create(firstLetter, font);
        using var paint = new SKPaint(font);
        paint.Color = textColor;
        paint.TextAlign = SKTextAlign.Center;

        var left = Math.Abs(blob.Bounds.Left);
        var y = imageSize - left;
        canvas.DrawText(blob, left, y, paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);

        return encoded.ToArray();
    }

    private async Task<Ok<InfoResponse>> MakeInfoResponse(DataLibrary.Models.User.User user)
    {
        var roles = await GetRolesForUser(user);

        return TypedResults.Ok(new InfoResponse
        {
            UserId = user.Id,
            UserSequenceId = user.UserSequenceId,
            Username = await _userManager.GetUserNameAsync(user) ?? throw new NotSupportedException("Username is null"),
            Roles = roles,
            RegisterDate = user.RegisterDate
        });
    }

    private async Task<Ok<ExtendedInfoResponse>> MakeExtendedInfoResponse(DataLibrary.Models.User.User user)
    {
        var roles = await GetRolesForUser(user);

        return TypedResults.Ok(new ExtendedInfoResponse
        {
            UserId = user.Id,
            UserSequenceId = user.UserSequenceId,
            Username = await _userManager.GetUserNameAsync(user) ?? throw new NotSupportedException("Username is null"),
            Email = await _userManager.GetEmailAsync(user) ?? throw new NotSupportedException("Email is null"),
            Roles = roles,
            RegisterDate = user.RegisterDate,
            IsLockedOut = await _userManager.IsLockedOutAsync(user),
            Subscriptions = user.Subscriptions.Select(MakeSubscriptionResponse)
        });
    }

    private async Task<OneOf<DataLibrary.Models.User.User, ProblemHttpResult>> GetCurrentUser()
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return TypedResults.Problem("Context not found", statusCode: StatusCodes.Status400BadRequest);
        }

        if (await GetCurrentUserFromHttpContext() is not { } user)
        {
            return TypedResults.Problem("No user found", statusCode: StatusCodes.Status400BadRequest);
        }

        return user;
    }

    private async Task<DataLibrary.Models.User.User?> GetCurrentUserFromHttpContext()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await GetUser(userId, true);
    }

    private async Task<OneOf<DataLibrary.Models.User.User, ProblemHttpResult>> GetUser(int userSqeunceId, bool fromCache)
    {

        var user = fromCache ? await _cacheService.GetAndSetCacheEntry(
            CacheKeys.MakeCacheKey(CacheKeys.User, userSqeunceId.ToString()), GetUserFromManager) :
                await GetUserFromManager();

        if (user is null)
        {
            return TypedResults.Problem("No user found", statusCode: StatusCodes.Status404NotFound);
        }

        return user;

        async Task<DataLibrary.Models.User.User?> GetUserFromManager()
        {
            return await _userManager.Users
                .Include(u => u.Subscriptions)
                .ThenInclude(s => s.SubscriptionType)
                .ThenInclude(st => st.Software)
                .FirstOrDefaultAsync(u => u.UserSequenceId == userSqeunceId);
        }
    }

    private ProblemHttpResult LogUpdateUserError(IdentityResult updateUserResult, string userId)
    {
        _logger.LogError("UpdateUser | Could not update User {UserId} {Errors}", userId,
            string.Join("\n", updateUserResult.Errors.Select(e => $"{e.Code} | {e.Description}")));
        return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
    }

    private Subscription MakeSubscription(SubscriptionType subscriptionType, string userId)
    {
        return new Subscription
        {
            UserId = userId,
            SubscriptionTypeId = subscriptionType.Id,
            StartDate = _dateTimeProvider.UtcNow,
            EndDate = _dateTimeProvider.UtcNow.AddDays(subscriptionType.LengthInDays)
        };
    }

    private static ValidationProblem CreateValidationProblem(string errorCode, string errorDescription) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> {
            { errorCode, [errorDescription] }
        });

    private static ValidationProblem CreateValidationProblem(params IdentityResult[] results)
    {
        var errorDictionary = new Dictionary<string, string[]>(results.Length);

        foreach (var result in results)
        {
            foreach (var error in result.Errors)
            {
                string[] newDescriptions;

                if (errorDictionary.TryGetValue(error.Code, out var descriptions))
                {
                    newDescriptions = new string[descriptions.Length + 1];
                    Array.Copy(descriptions, newDescriptions, descriptions.Length);
                    newDescriptions[descriptions.Length] = error.Description;
                }
                else
                {
                    newDescriptions = [error.Description];
                }

                errorDictionary[error.Code] = newDescriptions;
            }
        }

        return TypedResults.ValidationProblem(errorDictionary);
    }

    private async Task SendConfirmationEmail(DataLibrary.Models.User.User user, string email, bool isChange = false)
    {
        var code = isChange
            ? await _userManager.GenerateChangeEmailTokenAsync(user, email)
            : await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var userId = await _userManager.GetUserIdAsync(user);

        var url = $"{_appSettings.ClientPaths.BaseUrl}/{_appSettings.ClientPaths.ConfirmEmail}/{userId}/{code}";
        if (isChange)
        {
            url += $"/{email}";
        }

        var mailSettings = new MailSettings
        {
            Subject = "Confirm your email",
            TemplateName = "ConfirmMail",
            TemplateData = new
            {
                ConfirmEmailLink = url
            }
        };

        await _mailService.SendMail(mailSettings, email);
    }

    private static SKColor GetColorFromUsername(string userName)
    {
        const byte minValue = 50;

        var hash = userName.GetHashCode();

        var hue = (decimal)NormalizeHash(0, 360);
        var saturation = Math.Abs((decimal)NormalizeHash(0, 100) / 100);
        var value = Math.Abs((decimal)NormalizeHash(0, 100) / 100);

        var hi = Convert.ToInt32(Math.Floor((hue / 60))) % 6;
        var f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        var v = Math.Max(Convert.ToByte(value), minValue);
        var p = Math.Max(Convert.ToByte(value * (1 - saturation)), minValue);
        var q = Math.Max(Convert.ToByte(value * (1 - f * saturation)), minValue);
        var t = Math.Max(Convert.ToByte(value * (1 - (1 - f) * saturation)), minValue);

        return hi switch
        {
            0 => new SKColor(v, t, p),
            1 => new SKColor(q, v, p),
            2 => new SKColor(p, v, t),
            3 => new SKColor(p, q, v),
            4 => new SKColor(t, p, v),
            _ => new SKColor(v, p, q)
        };

        int NormalizeHash(int min, int max)
        {
            return (int)Math.Floor((decimal)((hash % (max - min)) + min));
        }
    }

    private async Task<IEnumerable<Role>> GetRolesForUser(DataLibrary.Models.User.User user)
    {
        var roles = new List<Role>();

        foreach (var roleString in await _userManager.GetRolesAsync(user))
        {
            if (!Enum.TryParse<Role>(roleString, out var role))
            {
                continue;
            }

            roles.Add(role);
        }

        return roles;
    }

    private SubscriptionResponse MakeSubscriptionResponse(Subscription subscription)
    {
        var timeRemaining = subscription.EndDate - _dateTimeProvider.UtcNow;
        if (timeRemaining < TimeSpan.Zero)
        {
            timeRemaining = TimeSpan.Zero;
        }

        return new SubscriptionResponse
        {
            SubscriptionId = subscription.Id,
            SoftwareName = subscription.SubscriptionType.Software.Name,
            SubscriptionTypeName = subscription.SubscriptionType.Name,
            EndDate = subscription.EndDate,
            TimeRemaining = timeRemaining,
            IsActive = timeRemaining > TimeSpan.Zero
        };
    }

    private async Task AddUserToRoleIfNecessary(DataLibrary.Models.User.User user, Role role)
    {
        var roleString = role.ToString();
        if (await _userManager.IsInRoleAsync(user, roleString))
        {
            return;
        }

        await _userManager.AddToRoleAsync(user, roleString);
    }
}