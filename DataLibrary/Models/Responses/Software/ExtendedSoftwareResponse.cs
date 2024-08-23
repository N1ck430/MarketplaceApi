namespace DataLibrary.Models.Responses.Software;

public class ExtendedSoftwareResponse : SoftwareResponse
{
    public ICollection<SubscriptionTypeResponse> SubscriptionTypes { get; set; } = [];
}