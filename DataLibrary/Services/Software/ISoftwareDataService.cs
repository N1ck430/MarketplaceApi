using DataLibrary.Models.Requests.Software;
using DataLibrary.Models.Software;

namespace DataLibrary.Services.Software;

public interface ISoftwareDataService
{
    Task<Models.Software.Software> AddSoftware(Models.Software.Software software);
    Task<Models.Software.Software?> GetSoftware(int id);
    Task<Models.Software.Software?> UpdateSoftware(UpdateSoftwareRequest updateSoftware);
    Task<SubscriptionType> AddSubscriptionType(SubscriptionType subscriptionType);
    Task<SubscriptionType?> GetSubscriptionType(int subscriptionTypeId);
    Task<SubscriptionType?> UpdateSubscriptionType(UpdateSubscriptionTypeRequest updateSubscriptionType);
    (IEnumerable<Models.Software.Software> softwareList, int totalCount) SearchSoftware(
        SoftwareSearchRequest searchRequest);

    Task<bool> DeleteSubscriptionType(int subscriptionTypeId);
    Task<bool> DeleteSoftware(int softwareId);
}