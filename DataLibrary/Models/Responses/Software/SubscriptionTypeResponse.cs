namespace DataLibrary.Models.Responses.Software;

public class SubscriptionTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int LengthInDays { get; set; }
}