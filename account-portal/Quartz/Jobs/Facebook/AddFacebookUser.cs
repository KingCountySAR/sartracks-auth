using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;

namespace SarData.Accounts.Quartz.Jobs.Facebook
{
  public class AddFacebookUser : IJob
  {
    public Task Execute(IJobExecutionContext context)
    {
      Console.WriteLine("Creating Facebook user");
      return Task.CompletedTask;
    }

    public static async Task<IJobDetail>Create(IScheduler scheduler, Dictionary<string, object> payload, params IJobDetail[] childrenJobs)
    {
      return await scheduler.CreateJob<AddFacebookUser>(payload, childrenJobs);
    }
  }
}
