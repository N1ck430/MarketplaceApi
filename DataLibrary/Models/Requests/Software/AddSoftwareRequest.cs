using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models.Requests.Software;

public class AddSoftwareRequest
{
    [Required]
    public string Name { get; set; } = null!;
}