using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using SarData.Accounts.Quartz.Jobs.Facebook;
using SarData.Accounts.Quartz.Jobs.GSuite;

namespace SarData.Accounts.Quartz.Jobs
{
  [DisallowConcurrentExecution]
  [PersistJobDataAfterExecution]
  public class OnboardMemberJob : IJob
  {
    public Task Execute(IJobExecutionContext context)
    {
      //int step = 0;
      //if (context.MergedJobDataMap.TryGetValue("step", out object obj))
      //{
      //  step = (int)(long)obj;
      //}
      string username = context.MergedJobDataMap.GetString("username");

      Console.WriteLine("Running OnboardMemberJob task " + username);

      return Task.CompletedTask;
      //Console.WriteLine($"#########################   Starting Task for {username} {step} {context.JobDetail.Key}");
      //await Task.Delay(TimeSpan.FromSeconds(3));


      //if (step == 3)
      //{
      //  Console.WriteLine("Finishing task for " + username);
      //}
      //else
      //{
      //  step++;
      //  context.JobDetail.JobDataMap.Put("step", step);
      //  context.Put("step", step);
      //  //await context.Scheduler.AddJob(context.JobDetail, true);
      //  throw new JobExecutionException($"Not finished yet {username} {step}");
      //  //var newJob = context.Scheduler.AddJob(context.JobDetail, true);
      //  //ITrigger t = context.Trigger;
      //}
    }

    public static async Task<IJobDetail> Create(IScheduler scheduler, Options options)
    {
      //var childJobs = await 
      //var newJob = JobBuilder.Create<OnboardMemberJob>()
      //.WithIdentity(Guid.NewGuid().ToString("N"))
      //.StoreDurably(true)
      //.RequestRecovery(true)
      //.Build();

      //newJob.JobDataMap.Put("username", options.Username);
      //newJob.JobDataMap.Put("email", options.PersonalEmail);

      //newJob.JobDataMap.Put(JobChainHandler.PayloadKey, payloadMap ?? new Dictionary<string, object>());

      //if (childrenJobs != null && childrenJobs.Length > 0)
      //{
      //  var jkList = childrenJobs.Select(job => job.Key).ToList();
      //  newJob.JobDataMap.Put(JobChainHandler.PayloadKey, payloadMap ?? new Dictionary<string, object>());
      //  newJob.JobDataMap.Put(JobChainHandler.NextJobKey, jkList);
      //}

      //await scheduler.AddJob(newJob, true);

      Dictionary<string, object> cloneOptions() => new Dictionary<string, object> { { "username", options.Username }, { "email", options.PersonalEmail }, { "first", options.FirstName }, { "last", options.LastName } };

      var setupFacebook = await AddFacebookUser.Create(scheduler, cloneOptions(), null);
      var setupGSuite = await AddGSuiteUserJob.Create(scheduler, cloneOptions(), setupFacebook);

      return await scheduler.CreateJob<OnboardMemberJob>(cloneOptions(), setupGSuite);

      //await scheduler.CreateJob<SetForwardingAddressJob>(null);
      //var waitJob = await scheduler.CreateJob<WaitForAddressConfirmationJob>(null, setJob);

      //var gsuiteJob = await scheduler.CreateJob<AddVerifiedAddressJob>(new Dictionary<string, object> { { "username", options.Username }, { "email", options.PersonalEmail } }, waitJob);

      //await scheduler.AddJob(gsuiteJob, true);

      //var createAccountJob = await scheduler.CreateJob<AddGSuiteUser>(cloneOptions(), );

      //return createAccountJob;

      //var setJob = await scheduler.CreateJob<SetForwardingAddressJob>(null);
      //var waitJob = await scheduler.CreateJob<WaitForAddressConfirmationJob>(null, setJob);

      //return await scheduler.CreateJob<AddVerifiedAddressJob>(null, waitJob);
    }

    public class Options
    {
      public string FirstName { get; set; }
      public string LastName { get; set; }
      public string PersonalEmail { get; set; }
      public string Username { get; set; }
    }
  }
}
