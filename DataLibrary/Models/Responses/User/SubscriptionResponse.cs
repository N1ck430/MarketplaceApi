using DataLibrary.Models.Responses.General;

namespace DataLibrary.Models.Responses.User;

public class SubscriptionResponse
{
    public int SubscriptionId { get; set; }
    public string SoftwareName { get; set; } = null!;
    public string SubscriptionTypeName { get; set; } = null!;
    public DateTime EndDate { get; set; }
    public TimeSpanResponse TimeRemaining { get; set; } = null!;
    public bool IsActive { get; set; }
}