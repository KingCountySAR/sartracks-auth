using System;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Logging;
using Quartz;

namespace SarData.Accounts.Quartz.Jobs.GSuite
{
  [PersistJobDataAfterExecution]
  public class SetForwardingAddressJob : IJob
  {
    private readonly GSuiteApi api;
    private readonly ILogger<SetForwardingAddressJob> logger;

    public SetForwardingAddressJob(GSuiteApi api, ILogger<SetForwardingAddressJob> logger)
    {
      this.api = api;
      this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      string username = context.MergedJobDataMap.GetString("username");
      string email = context.MergedJobDataMap.GetString("email");

      var service = api.GetGmailService(username);
      var forward = await service.Users.Settings.GetAutoForwarding(username).ExecuteAsync();
      if (forward.Disposition != "trash" || !forward.EmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase) || !(forward.Enabled ?? false))
      {
        logger.LogInformation("Setting forwarding address for " + username);
        await service.Users.Settings.UpdateAutoForwarding(new AutoForwarding
        {
          Disposition = "trash",
          EmailAddress = email,
          Enabled = true
        }, username).ExecuteAsync();
      }
    }
  }
}
