using DataLibrary.Models.User;

namespace DataLibrary.Models.Responses.User;

public class InfoResponse
{
    public string UserId { get; set; } = null!;
    public int UserSequenceId { get; set; }
    public string Username { get; set; } = null!;
    public IEnumerable<Role> Roles { get; set; } = null!;
    public DateTime RegisterDate { get; set; }
}