using DataLibrary.Models.Application;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using System.Net.Mail;
using System.Reflection;
using System.Text.Json;

namespace MarketplaceApi.Services.Mail;

public class MailService : IMailService
{
    private readonly MailServerSettings _mailServerSettings;
    private readonly ILogger<MailService> _logger;
    private readonly string _mailDirectory;
    private readonly string _templateDirectory;

    public MailService(
        IOptions<ApplicationSettings> appSettings,
        ILogger<MailService> logger,
        IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _mailServerSettings = appSettings.Value.MailSettings;

        _templateDirectory = Path.Combine(hostEnvironment.ContentRootPath, "Templates", "MailTemplates");

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        _mailDirectory =
            Path.Combine(assemblyLocation[..(assemblyLocation.LastIndexOf("\\", StringComparison.Ordinal))],
                "Mails");
        if (!Directory.Exists(_mailDirectory))
        {
            Directory.CreateDirectory(_mailDirectory);
        }
    }

    public async Task<bool> SendMail(MailSettings mailSettings, string mailTo)
    {
        try
        {
            var templateText =
                await File.ReadAllTextAsync(Path.Combine(_templateDirectory, "Html",
                    $"{mailSettings.TemplateName}.html"));
            var styleText = await File.ReadAllTextAsync(Path.Combine(_templateDirectory, "Styles", "Theme.css"));

            foreach (var propertyInfo in mailSettings.TemplateData.GetType().GetProperties())
            {
                templateText = templateText.Replace($"{{{propertyInfo.Name}}}",
                    propertyInfo.GetValue(mailSettings.TemplateData)?.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            templateText = $"<style>{styleText}</style> {templateText}";

            templateText = new PreMailer.Net.PreMailer(templateText).MoveCssInline().Html;

            await SendMail(mailTo, mailSettings.Subject, templateText);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendMail({mailSetting}, {mailTo}", JsonSerializer.Serialize(mailSettings), mailTo);
            return false;
        }
    }

    private async Task SendMail(string mailTo, string subject, string html)
    {
        if (_mailServerSettings.SaveMailsToFile)
        {
            await SendMailToFolder(mailTo, subject, html);
        }
        else
        {
            await SendMailViaSerer(mailTo, subject, html);
        }
    }

    private async Task SendMailToFolder(string mailTo, string subject, string html)
    {
        using var emailMessage = new MailMessage(_mailServerSettings.MailFrom, mailTo);
        emailMessage.Subject = subject;
        emailMessage.Body = html;
        emailMessage.IsBodyHtml = true;
        using var client = new SmtpClient(_mailServerSettings.Server, _mailServerSettings.Port);
        client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
        client.PickupDirectoryLocation = _mailDirectory;

        await client.SendMailAsync(emailMessage);
    }

    private async Task SendMailViaSerer(string mailTo, string subject, string html)
    {
        using var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(_mailServerSettings.Username, _mailServerSettings.MailFrom));
        emailMessage.To.Add(new MailboxAddress(string.Empty, mailTo));

        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart(TextFormat.Html)
        {
            Text = html
        };

        using var client = new MailKit.Net.Smtp.SmtpClient();

        await client.ConnectAsync(_mailServerSettings.Server, _mailServerSettings.Port);
        await client.AuthenticateAsync(_mailServerSettings.MailFrom, _mailServerSettings.Password);
        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }
}