namespace DataLibrary.Models.Application;

public class ApplicationSettings
{
    public string ClientOrigins { get; set; } = null!;
    public MailServerSettings MailSettings { get; set; } = null!;
    public ClientPaths ClientPaths { get; set; } = null!;
}

public class MailServerSettings
{
    public bool SaveMailsToFile { get; set; }
    public string Username { get; set; } = null!;
    public string MailFrom { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Server { get; set; } = null!;
    public int Port { get; set; }
}

public class ClientPaths
{
    public string BaseUrl { get; set; } = null!;
    public string ConfirmEmail { get; set; } = null!;
    public string ResetPassword { get; set; } = null!;
}