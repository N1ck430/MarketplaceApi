using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models.Requests.Software;

public class AddSubscriptionTypeRequest
{
    public int SoftwareId { get; set; }
    public int LengthInDays { get; set; }
    [Required]
    public string Name { get; set; } = null!;
}