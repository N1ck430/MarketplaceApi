namespace DataLibrary.Models.Software;

public class Subscription
{
    public int Id { get; set; }
    public int SubscriptionTypeId { get; set; }
    public SubscriptionType SubscriptionType { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public User.User User { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}