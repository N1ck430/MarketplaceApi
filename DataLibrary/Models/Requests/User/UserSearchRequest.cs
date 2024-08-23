using DataLibrary.Models.Requests.General;

namespace DataLibrary.Models.Requests.User;

public class UserSearchRequest : BaseSearchRequest
{
    public UserSearchOrder UserSearchOrder { get; set; } = UserSearchOrder.Id;
    public bool OrderDesc { get; set; } = false;
}

public enum UserSearchOrder
{
    Id,
    Name,
    RegisterDate
}