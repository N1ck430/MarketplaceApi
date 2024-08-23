using DataLibrary.Models.Requests.Software;
using DataLibrary.Models.Responses.General;
using DataLibrary.Models.Responses.Software;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MarketplaceApi.Services.Software;

public interface ISoftwareService
{
    Results<Ok<ListResponse<SoftwareResponse>>, ProblemHttpResult> SearchSoftware(SoftwareSearchRequest searchRequest);
    Task<Results<Ok<ExtendedSoftwareResponse>, NotFound>> GetSoftware(int id);
    Task<Results<Ok<ExtendedSoftwareResponse>, NotFound>> UpdateSoftware(UpdateSoftwareRequest updateSoftware);
    Task<Results<Ok<SoftwareResponse>, ProblemHttpResult>> AddSoftware(AddSoftwareRequest addSoftware);

    Task<Results<Ok<SubscriptionTypeResponse>, NotFound>> UpdateSubscriptionType(
        UpdateSubscriptionTypeRequest updateSubscriptionTypeRequest);

    Task<Results<Ok<SubscriptionTypeResponse>, ProblemHttpResult>> AddSubscriptionType(
        AddSubscriptionTypeRequest addSubscriptionTypeRequest);

    Task<Results<Ok, ProblemHttpResult>> DeleteSubscriptionType(int subscriptionTypeId);
    Task<Results<Ok, ProblemHttpResult>> DeleteSoftware(int softwareId);
}