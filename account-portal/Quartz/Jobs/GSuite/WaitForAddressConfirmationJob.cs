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
  public class WaitForAddressConfirmationJob : IJob
  {
    private readonly GSuiteApi api;
    private readonly ILogger<WaitForAddressConfirmationJob> logger;

    public WaitForAddressConfirmationJob(GSuiteApi api, ILogger<WaitForAddressConfirmationJob> logger)
    {
      this.api = api;
      this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      string username = context.MergedJobDataMap.GetString("username");
      string email = context.MergedJobDataMap.GetString("email");

      var service = api.GetGmailService(username);
      var forwardingAddresses = await service.Users.Settings.ForwardingAddresses.List(username).ExecuteAsync();

      var existing = (forwardingAddresses?.ForwardingAddresses ?? new List<ForwardingAddress>())
                      .FirstOrDefault(f => f.ForwardingEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
      
      if (existing.VerificationStatus != "accepted")
      {
        logger.LogInformation($"Waiting for {username} to accept forwarding address");
        context.JobDetail.JobDataMap.Put("_refire", 30);
      }
    }
  }
}