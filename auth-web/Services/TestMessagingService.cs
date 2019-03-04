using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using SarData.Common.Apis;
using SarData.Common.Apis.Messaging;

namespace SarData.Auth.Services
{
  // This class is used by the application to send email for account confirmation and password reset.
  // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
  public class TestMessagingService : IMessagingApi
  {
    private readonly IHostingEnvironment env;

    public TestMessagingService(IHostingEnvironment env)
    {
      this.env = env;
    }

    public async Task SendEmail(SendEmailRequest request)
    {
      string[] lines = new[] { "Date: " + DateTime.Now, "TO: " + request.To, "SUBJ: " + request.Subject, request.Message, string.Empty };
      File.AppendAllLines(GetPath("logs\\sent-mail.log"), lines);

      var client = new SmtpClient { DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory, PickupDirectoryLocation = GetPath("logs\\sent-mail\\") };
      var mail = new MailMessage("example@example.com", request.To, request.Subject, request.Message);
      await client.SendMailAsync(mail);
    }

    public Task SendText(string phone, string message)
    {
      File.AppendAllLines(GetPath("logs\\sent-sms.log"), new[] { $"{phone}: {message}" });
      return Task.CompletedTask;
    }

    private string GetPath(string path)
    {
      path = Path.Combine(env.ContentRootPath, path);
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      return path;
    }
  }
}
