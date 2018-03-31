using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace SarData.Auth.Services
{
  // This class is used by the application to send email for account confirmation and password reset.
  // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
  public class TestMessagingService : IMessagingService
  {
    private readonly IHostingEnvironment env;

    public TestMessagingService(IHostingEnvironment env)
    {
      this.env = env;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
      await File.AppendAllLinesAsync(GetPath("logs\\sent-mail.log"), new[] { "TO: " + email, "SUBJ: " + subject, message, string.Empty });

      var client = new SmtpClient { DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory, PickupDirectoryLocation = GetPath("logs\\sent-mail\\") };
      var mail = new MailMessage("example@example.com", email, subject, message);
      await client.SendMailAsync(mail);
    }

    public async Task SendTextAsync(string phone, string message)
    {
      await File.AppendAllLinesAsync(GetPath("logs\\sent-sms.log"), new[] { $"{phone}: {message}" });
    }

    private string GetPath(string path)
    {
      path = Path.Combine(env.ContentRootPath, path);
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      return path;
    }
  }
}
