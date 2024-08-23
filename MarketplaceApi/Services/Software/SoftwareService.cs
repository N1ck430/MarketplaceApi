using System.Text.Json;
using DataLibrary.Models.Requests.Software;
using DataLibrary.Models.Responses.General;
using DataLibrary.Models.Responses.Software;
using DataLibrary.Models.Software;
using DataLibrary.Services.Software;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MarketplaceApi.Services.Software;

public class SoftwareService : ISoftwareService
{
    private readonly ISoftwareDataService _softwareDataService;
    private readonly ILogger<SoftwareService> _logger;

    public SoftwareService(
        ISoftwareDataService softwareDataService,
        ILoggerFactory loggerFactory)
    {
        _softwareDataService = softwareDataService;
        _logger = loggerFactory.CreateLogger<SoftwareService>();
    }

    public Results<Ok<ListResponse<SoftwareResponse>>, ProblemHttpResult> SearchSoftware(
        SoftwareSearchRequest searchRequest)
    {
        try
        {
            var (softwareList, totalCount) = _softwareDataService.SearchSoftware(searchRequest);
            var response = softwareList.Select(s => new SoftwareResponse
            {
                Id = s.Id,
                Name = s.Name,
                SubscriptionTypesCount = s.SubscriptionTypes.Count
            });

            return TypedResults.Ok(new ListResponse<SoftwareResponse>
            {
                ListEntries = response,
                Count = totalCount
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SearchSoftware | Could not search software {searchRequest}",
                JsonSerializer.Serialize(searchRequest));
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<Results<Ok<SoftwareResponse>, ProblemHttpResult>> AddSoftware(AddSoftwareRequest addSoftware)
    {
        try
        {
            var software = new DataLibrary.Models.Software.Software
            {
                Name = addSoftware.Name
            };

            var response = await _softwareDataService.AddSoftware(software);

            return TypedResults.Ok(new SoftwareResponse
            {
                Id = response.Id,
                Name = response.Name,
                SubscriptionTypesCount = 0
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AddSoftware | Could not add Software {softwareName}", addSoftware.Name);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<Results<Ok<ExtendedSoftwareResponse>, NotFound>> GetSoftware(int id)
    {
        var software = await _softwareDataService.GetSoftware(id);
        
        if (software is null)
        {
            return TypedResults.NotFound();
        }

        return MakeExtendedSoftwareResponse(software);
    }

    public async Task<Results<Ok<ExtendedSoftwareResponse>, NotFound>> UpdateSoftware(
        UpdateSoftwareRequest updateSoftware)
    {
        var result = await _softwareDataService.UpdateSoftware(updateSoftware);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return MakeExtendedSoftwareResponse(result);
    }

    public async Task<Results<Ok<SubscriptionTypeResponse>, ProblemHttpResult>> AddSubscriptionType(
        AddSubscriptionTypeRequest addSubscriptionTypeRequest)
    {
        try
        {
            var result = await _softwareDataService.AddSubscriptionType(new SubscriptionType
            {
                SoftwareId = addSubscriptionTypeRequest.SoftwareId,
                Name = addSubscriptionTypeRequest.Name,
                LengthInDays = addSubscriptionTypeRequest.LengthInDays
            });

            return MakeSubscriptionTypeResponse(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "AddSubscriptionType | Could not add SubscriptionType {subscriptionTypeName} for software {softwareId}",
                addSubscriptionTypeRequest.Name, addSubscriptionTypeRequest.SoftwareId);
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<Results<Ok<SubscriptionTypeResponse>, NotFound>> UpdateSubscriptionType(
        UpdateSubscriptionTypeRequest updateSubscriptionTypeRequest)
    {
        var result = await _softwareDataService.UpdateSubscriptionType(updateSubscriptionTypeRequest);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return MakeSubscriptionTypeResponse(result);
    }

    public async Task<Results<Ok, ProblemHttpResult>> DeleteSoftware(int softwareId)
    {
        var result = await _softwareDataService.DeleteSoftware(softwareId);

        return result ? TypedResults.Ok() : TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
    }

    public async Task<Results<Ok, ProblemHttpResult>> DeleteSubscriptionType(int subscriptionTypeId)
    {
        var result = await _softwareDataService.DeleteSubscriptionType(subscriptionTypeId);

        return result ? TypedResults.Ok() : TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest);
    }

    private static Ok<SubscriptionTypeResponse> MakeSubscriptionTypeResponse(SubscriptionType result)
    {
        return TypedResults.Ok(new SubscriptionTypeResponse
        {
            Id = result.Id,
            LengthInDays = result.LengthInDays,
            Name = result.Name
        });
    }

    private static Ok<ExtendedSoftwareResponse> MakeExtendedSoftwareResponse(DataLibrary.Models.Software.Software software)
    {
        return TypedResults.Ok(new ExtendedSoftwareResponse
        {
            Id = software.Id,
            Name = software.Name,
            SubscriptionTypesCount = software.SubscriptionTypes.Count,
            SubscriptionTypes = software.SubscriptionTypes.Select(st => new SubscriptionTypeResponse
            {
                Id = st.Id,
                Name = st.Name,
                LengthInDays = st.LengthInDays
            }).ToList()
        });
    }
}