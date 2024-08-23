namespace DataLibrary.Models.Requests.User;

public class ConfirmEmailRequest
{
    public string UserId { get; set; } = null!;
    public string Code { get; set; } = null!;
}