using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Accounts.Quartz.Jobs.GSuite
{
  [PersistJobDataAfterExecution]
  public class AddVerifiedAddressJob : IJob
  {
    private readonly GSuiteApi api;
    private readonly ILogger<AddVerifiedAddressJob> logger;

    public AddVerifiedAddressJob(GSuiteApi api, ILogger<AddVerifiedAddressJob> logger)
    {
      this.api = api;
      this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      string username = context.MergedJobDataMap.GetString("username");
      string email = context.MergedJobDataMap.GetString("email");

      try
      {
        var service = api.GetGmailService(username);
        var forwardingAddresses = await service.Users.Settings.ForwardingAddresses.List(username).ExecuteAsync();

        var existing = (forwardingAddresses?.ForwardingAddresses ?? new List<ForwardingAddress>())
                        .FirstOrDefault(f => f.ForwardingEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
          logger.LogInformation("Adding forwarding address to " + username);
          await service.Users.Settings.ForwardingAddresses.Create(new ForwardingAddress { ForwardingEmail = email }, username).ExecuteAsync();
        }
      }
      catch (TokenResponseException e)
      {
        logger.LogWarning("Need to reschedule job.", e);
        context.JobDetail.JobDataMap.Put("_refire", 10);
      }
    }
  }
}
