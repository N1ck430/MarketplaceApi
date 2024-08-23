using DataLibrary.Models.Requests.User;
using DataLibrary.Models.Responses.General;
using DataLibrary.Models.Responses.User;
using DataLibrary.Models.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MarketplaceApi.Services.User;

public interface IUserService
{
    Task<Results<Ok, ValidationProblem>> Register(RegisterRequest register);
    Task<Results<Ok<AccessTokenResponse>, ProblemHttpResult, IResult>> Login(LoginRequest login);

    Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
        Refresh(string refreshToken);
    Task<Results<Ok, ProblemHttpResult>> ConfirmEmail(ConfirmEmailRequest confirmEmail);

    Task<Results<Ok<ExtendedInfoResponse>, ProblemHttpResult>> GetCurrentUserInfo();
    Task<Results<Ok, ValidationProblem>> ResetPassword(ResetPasswordRequest resetRequest);
    Results<Ok<ListResponse<InfoResponse>>, ProblemHttpResult> SearchUsers(UserSearchRequest searchRequest);

    Task<Results<Ok, ValidationProblem>> LockOutUser(string userId);

    Task<Results<Ok<ExtendedInfoResponse>, ProblemHttpResult>> GetExtendedUserInfo(int userSequenceId);

    Task<Results<Ok<InfoResponse>, ProblemHttpResult>> GetUserInfo(int userSequenceId);

    Task<DataLibrary.Models.User.User?> GetUser(string userId, bool fromCache);

    Task<Results<Ok, ProblemHttpResult>> UpdateUserRoles(string userId, List<Role> roles);

    Task<Results<Ok, ProblemHttpResult>> AddSubscriptionToUser(string userId, int subscriptionTypeId);

    void RemoveUserFromCache(DataLibrary.Models.User.User user);

    byte[] GenerateProfilePicture(string userName);
}