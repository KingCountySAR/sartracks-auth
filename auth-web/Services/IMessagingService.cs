using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Services
{
    public interface IMessagingService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
