namespace DataLibrary.Models.Software;

public class SubscriptionType
{
    public int Id { get; set; }
    public int SoftwareId { get; set; }
    public Software Software { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int LengthInDays { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}