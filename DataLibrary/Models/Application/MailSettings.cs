namespace DataLibrary.Models.Application;

public class MailSettings
{
    public string TemplateName { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public object TemplateData { get; set; } = null!;
}