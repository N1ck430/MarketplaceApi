using Microsoft.AspNetCore.Http.HttpResults;

namespace MarketplaceApi.Services.Image;

public interface IImageService
{
    public Task<Results<FileContentHttpResult, ProblemHttpResult>> GetProfilePicture(string userId);
}