namespace DataLibrary.Models.Requests.Software;

public class UpdateSoftwareRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}