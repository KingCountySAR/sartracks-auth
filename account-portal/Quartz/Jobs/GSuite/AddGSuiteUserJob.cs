using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Google;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Admin.Directory.directory_v1.Data;
using Microsoft.Extensions.Logging;
using Quartz;

namespace SarData.Accounts.Quartz.Jobs.GSuite
{
  public class AddGSuiteUserJob : IJob
  {
    private readonly GSuiteApi api;
    private readonly ILogger<AddGSuiteUserJob> logger;

    public AddGSuiteUserJob(GSuiteApi api, ILogger<AddGSuiteUserJob> logger)
    {
      this.api = api;
      this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      logger.LogInformation("Starting request to create user");
      UsersResource.InsertRequest newUserRequest = api.GetDirectoryService().Users.Insert(new User
      {
        PrimaryEmail = context.MergedJobDataMap.GetString("username"),
        RecoveryEmail = context.MergedJobDataMap.GetString("email"),
        Name = new UserName
        {
          GivenName = context.MergedJobDataMap.GetString("first"),
          FamilyName = context.MergedJobDataMap.GetString("last")
        },
        OrgUnitPath = "/Test-members",
        Password = Guid.NewGuid().ToString()
      });

      try
      {
        User newUser = await newUserRequest.ExecuteAsync();
        logger.LogInformation("Created GSuite user " + context.MergedJobDataMap.GetString("username"));
      }
      catch (GoogleApiException e)
      {
        if (e.HttpStatusCode == System.Net.HttpStatusCode.Conflict)
        {
          logger.LogWarning($"Tried to create user - conflict: {JsonSerializer.Serialize(context.JobDetail.JobDataMap)}");
        }
        else
        {
          throw e;
        }
      }
    }

    public static async Task<IJobDetail> Create(IScheduler scheduler, Dictionary<string, object> payload, params IJobDetail[] childrenJobs)
    {
      var task = await scheduler.CreateJob<SetForwardingAddressJob>(payload, childrenJobs);
      task = await scheduler.CreateJob<WaitForAddressConfirmationJob>(payload, task);
      task = await scheduler.CreateJob<AddVerifiedAddressJob>(payload, task);
      task = await scheduler.CreateJob<AddGSuiteUserJob>(payload, task);
      return task;
    }
  }
}
