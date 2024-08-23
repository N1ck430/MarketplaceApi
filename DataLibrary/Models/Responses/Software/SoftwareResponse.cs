namespace DataLibrary.Models.Responses.Software;

public class SoftwareResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int SubscriptionTypesCount { get; set; }
}