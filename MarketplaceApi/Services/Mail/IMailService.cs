using DataLibrary.Models.Application;

namespace MarketplaceApi.Services.Mail;

public interface IMailService
{
    public Task<bool> SendMail(MailSettings mailSettings, string mailTo);
}