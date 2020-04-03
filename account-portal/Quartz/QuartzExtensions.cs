using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Accounts.Quartz
{
  public static class QuartzExtensions
  {
    public static IScheduler AddQuartzAndStart(this IServiceCollection services, string filename)
    {
      string connstr = BuildDatabase(filename);

      NameValueCollection quartzProps = new NameValueCollection
      {
        { "quartz.serializer.type", "json" },
        { "quartz.threadPool.type", "Quartz.Simpl.SimpleThreadPool, Quartz" },
        { "quartz.threadPool.threadCount", "10" },
        { "quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" },
        { "quartz.jobStore.misfireThreshold", "60000" },
        { "quartz.jobStore.lockHandler.type", "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz" },
        { "quartz.jobStore.dataSource", "default" },
        { "quartz.jobStore.tablePrefix", "qrtz_" },
        { "quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.SQLiteDelegate, Quartz" },
        { "quartz.dataSource.default.provider", "SQLite-Microsoft" },
        { "quartz.dataSource.default.connectionString", connstr }
      };
      StdSchedulerFactory factory = new StdSchedulerFactory(quartzProps);
      IScheduler scheduler = factory.GetScheduler().GetAwaiter().GetResult();
      services.AddSingleton(scheduler);

      scheduler.ListenerManager.AddJobListener(new JobChainHandler(), GroupMatcher<JobKey>.AnyGroup());
      scheduler.StartDelayed(TimeSpan.FromSeconds(5));
      return scheduler;
    }

    public static IApplicationBuilder UseQuartzDependencyInjection(this IApplicationBuilder app)
    {
      app.ApplicationServices.GetRequiredService<IScheduler>().JobFactory = new DIJobFactory(app.ApplicationServices);
      return app;
    }

    private static string BuildDatabase(string dbFilename)
    {
      if (!File.Exists(dbFilename))
      {
        File.WriteAllBytes(dbFilename, new byte[0]);
      }

      var connstr = new SqliteConnectionStringBuilder() { DataSource = dbFilename }.ToString();
      SqliteConnection conn = new SqliteConnection(connstr);
      conn.Open();
      long tableCount = 0;
      using (SqliteCommand cmd = new SqliteCommand("SELECT COUNT(1) FROM sqlite_master WHERE type='table';", conn))
      {
        tableCount = (long)cmd.ExecuteScalar();
      }
      if (tableCount == 0)
      {
        var assembly = typeof(Startup).Assembly;
        string sql;

        using (var sr = new StreamReader(assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(f => f.EndsWith(".quartz.sql")))))
        {
          sql = sr.ReadToEnd();
        }
        //"quartz.sql");
        Console.WriteLine(sql);
        using (var cmd = new SqliteCommand(sql, conn))
        {
          cmd.ExecuteNonQuery();
        }
      }
      conn.Close();
      return connstr;
    }

    public static async Task<IJobDetail> CreateJob<TJob>(this IScheduler scheduler, Dictionary<string, object> payloadMap, params IJobDetail[] childrenJobs) where TJob : IJob
    {
      var newJob = JobBuilder.Create<TJob>()
          .WithIdentity(Guid.NewGuid().ToString("N"))
          .StoreDurably(true)
          .RequestRecovery(true)
          .Build();

      foreach (var pair in payloadMap)
      {
        newJob.JobDataMap.Put(pair.Key, pair.Value);
      }

      if (childrenJobs != null && childrenJobs.Length > 0)
      {
        var jkList = childrenJobs.Select(job => job.Key).ToList();
        newJob.JobDataMap.Put(JobChainHandler.NextJobKey, jkList);
      }

      await scheduler.AddJob(newJob, false);
      
      return newJob;
    }
  }
}