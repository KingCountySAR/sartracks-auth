using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Services
{
  public class MessagingService : IMessagingService
  {
    public Task SendEmailAsync(string email, string subject, string message)
    {
      throw new NotImplementedException();
    }

    public Task SendTextAsync(string phone, string message)
    {
      throw new NotImplementedException();
    }
  }
}
