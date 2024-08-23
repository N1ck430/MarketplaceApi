using DataLibrary.Models.User;
using MarketplaceApi.Services.Image;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = nameof(Role.User))]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(
        IImageService imageService,
        ILoggerFactory loggerFactory)
    {
        _imageService = imageService;
        _logger = loggerFactory.CreateLogger<ImageController>();
    }

    [HttpGet("{userId:required}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProfilePicture(string userId)
    {
        var result = await _imageService.GetProfilePicture(userId);
        return result;
    }
}