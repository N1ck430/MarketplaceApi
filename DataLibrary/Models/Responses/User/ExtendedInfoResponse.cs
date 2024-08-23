namespace DataLibrary.Models.Responses.User;

public class ExtendedInfoResponse : InfoResponse
{
    public string Email { get; set; } = null!;
    public bool IsLockedOut { get; set; }
    public IEnumerable<SubscriptionResponse> Subscriptions { get; set; } = null!;
}