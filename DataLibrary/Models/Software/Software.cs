namespace DataLibrary.Models.Software;

public class Software
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<SubscriptionType> SubscriptionTypes { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}