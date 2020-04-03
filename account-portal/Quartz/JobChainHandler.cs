using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SarData.Accounts.Quartz
{
  public class JobChainHandler : IJobListener
  {
    public const string NextJobKey = "_nextJobKey";

    public string Name => "JobChainHandler";

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct)
    {
      return Task.FromResult<object>(null);
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken ct)
    {
      return Task.FromResult<object>(null);
    }

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken ct)
    {
      if (jobException != null)
      {
        return;
      }

      if (context == null)
      {
        throw new ArgumentNullException("Completed job does not have valid Job Execution Context");
      }

      var finishedJob = context.JobDetail;

      if (finishedJob.JobDataMap.Contains("_refire"))
      {
        var refire = finishedJob.JobDataMap.GetInt("_refire");
        finishedJob.JobDataMap.Remove("_refire");

        var trigger = TriggerBuilder.Create()
          .WithIdentity(Guid.NewGuid().ToString(), "group1")
          .StartAt(DateBuilder.FutureDate(refire, IntervalUnit.Second))
          .ForJob(finishedJob.Key)
          .Build();
        await context.Scheduler.ScheduleJob(trigger);
        return;
      }
      else
      {
        Log.Logger.Debug("Task is finished. Deleting");
        await context.Scheduler.DeleteJob(finishedJob.Key);
      }

      var childJobs = finishedJob.JobDataMap.Get(NextJobKey) as List<JobKey> ?? new List<JobKey>();

      if (childJobs.Count == 0)
      {
        await context.Scheduler.DeleteJob(finishedJob.Key);
        return;
      }

      foreach (var jobKey in childJobs)
      {
        var newJob = await context.Scheduler.GetJobDetail(jobKey);

        if (newJob == null)
        {
          Debug.WriteLine($"Could not find Job with ID: {jobKey}");
          continue;
        }

        await context.Scheduler.AddJob(newJob, true, false);
        await context.Scheduler.TriggerJob(jobKey);
      }
    }
  }
}
