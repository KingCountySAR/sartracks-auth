using System.Threading.Tasks;

namespace SarData.Auth.Services
{
  public interface IMessagingService
  {
    Task SendEmailAsync(string email, string subject, string message);
    Task SendTextAsync(string phone, string message);
  }
}
