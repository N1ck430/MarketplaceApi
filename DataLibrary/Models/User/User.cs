using DataLibrary.Models.Software;
using Microsoft.AspNetCore.Identity;

namespace DataLibrary.Models.User;

public class User : IdentityUser
{
    public int UserSequenceId { get; set; }
    public DateTime RegisterDate { get; set; }
    public DateTime LastLoginDate { get; set; }
    public byte[] ProfilePicture { get; set; } = null!;
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}