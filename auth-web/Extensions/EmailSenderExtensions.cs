using System.Text.Encodings.Web;
using System.Threading.Tasks;
using SarData.Common.Apis.Messaging;

namespace SarData.Auth.Services
{
  public static class EmailSenderExtensions
  {
    public static Task SendEmailConfirmationAsync(this IMessagingApi emailSender, string email, string link)
    {
      return emailSender.SendEmail(email, "Confirm your email",
          $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
    }
  }
}
