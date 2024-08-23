using DataLibrary.Models.Requests.General;

namespace DataLibrary.Models.Requests.Software;

public class SoftwareSearchRequest : BaseSearchRequest
{
    public SoftwareSearchOrder SoftwareSearchOrder { get; set; } = SoftwareSearchOrder.Id;
    public bool OrderDesc { get; set; } = false;
}

public enum SoftwareSearchOrder
{
    Id,
    Name
}