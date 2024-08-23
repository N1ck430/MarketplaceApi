namespace DataLibrary.Models.Requests.User;

public class ResetPasswordRequest
{
    public string? Email { get; set; }
    public string? UserId { get; set; }
    public string? ResetCode { get; set; }
    public string? Password { get; set; }
}