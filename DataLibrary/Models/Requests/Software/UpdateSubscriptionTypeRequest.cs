namespace DataLibrary.Models.Requests.Software;

public class UpdateSubscriptionTypeRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int LengthInDays { get; set; }
}