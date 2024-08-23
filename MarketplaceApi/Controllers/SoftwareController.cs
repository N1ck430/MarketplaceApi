using DataLibrary.Models.Requests.Software;
using DataLibrary.Models.Responses.General;
using DataLibrary.Models.Responses.Software;
using DataLibrary.Models.User;
using MarketplaceApi.Services.Software;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MarketplaceApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = nameof(Role.User))]
public class SoftwareController : ControllerBase
{
    private readonly ISoftwareService _softwareService;
    private readonly ILogger<SoftwareController> _logger;

    public SoftwareController(
        ILoggerFactory loggerFactory, ISoftwareService softwareService)
    {
        _softwareService = softwareService;
        _logger = loggerFactory.CreateLogger<SoftwareController>();
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(ListResponse<SoftwareResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IResult SearchSoftware(SoftwareSearchRequest searchRequest)
    {
        try
        {
            var result = _softwareService.SearchSoftware(searchRequest);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SearchSoftware | Could not search software {searchRequest}",
                JsonSerializer.Serialize(searchRequest));
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(SoftwareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddSoftware(AddSoftwareRequest addSoftware)
    {
        try
        {
            var result = await _softwareService.AddSoftware(addSoftware);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AddSoftware | Could not add Software {softwareName}", addSoftware.Name);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExtendedSoftwareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSoftware(int id)
    {
        try
        {
            var result = await _softwareService.GetSoftware(id);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSoftware | Could not get Software {softwareId}", id);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPatch]
    [ProducesResponseType(typeof(ExtendedSoftwareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateSoftware(UpdateSoftwareRequest updateSoftware)
    {
        try
        {
            var result = await _softwareService.UpdateSoftware(updateSoftware);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "updateSoftware | Could not update Software {updateSoftware}",
                JsonSerializer.Serialize(updateSoftware));
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddSubscriptionType(AddSubscriptionTypeRequest addSubscriptionType)
    {
        try
        {
            var result = await _softwareService.AddSubscriptionType(addSubscriptionType);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "AddSubscriptionType | Could not add SubscriptionType {subscriptionTypeName} for Software {softwareId}",
                addSubscriptionType.Name, addSubscriptionType.SoftwareId);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateSubscriptionType(UpdateSubscriptionTypeRequest updateSubscriptionTypeRequest)
    {
        try
        {
            var result = await _softwareService.UpdateSubscriptionType(updateSubscriptionTypeRequest);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "UpdateSubscriptionType | Could not update SubscriptionType {subscriptionTypeId} {subscriptionTypeName}",
                updateSubscriptionTypeRequest.Id, updateSubscriptionTypeRequest.Name);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpDelete("{softwareId:int}")]
    [ProducesResponseType(typeof(SubscriptionTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteSoftware(int softwareId)
    {
        try
        {
            var result = await _softwareService.DeleteSoftware(softwareId);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DeleteSoftware | Could not delete software {softwareId}", softwareId);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpDelete("{subscriptionTypeId:int}")]
    [ProducesResponseType(typeof(SubscriptionTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteSubscriptionType(int subscriptionTypeId)
    {
        try
        {
            var result = await _softwareService.DeleteSubscriptionType(subscriptionTypeId);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DeleteSubscriptionType | Could not delete subscriptionType {subscriptionTypeId}",
                subscriptionTypeId);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }
}