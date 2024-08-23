using AngleSharp.Io;
using MarketplaceApi.Services.Cache;
using MarketplaceApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MarketplaceApi.Services.Image;

public class ImageService : IImageService
{
    private readonly IUserService _uerService;
    private readonly ICacheService _cacheService;

    public ImageService(
        IUserService userService,
        ICacheService cacheService)
    {
        _uerService = userService;
        _cacheService = cacheService;
    }

    public async Task<Results<FileContentHttpResult, ProblemHttpResult>> GetProfilePicture(string userId)
    {
        var imageBytes = (await _uerService.GetUser(userId, true))?.ProfilePicture;

        if (imageBytes is null)
        {
            return TypedResults.Problem("Image not found", statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.File(imageBytes, MimeTypeNames.Png);
    }
}