using System;
using System.Linq;
using Quartz;
using Quartz.Spi;

namespace SarData.Accounts.Quartz
{
  public class DIJobFactory : IJobFactory
  {
    private readonly IServiceProvider services;

    public DIJobFactory(IServiceProvider services)
    {
      this.services = services;
    }
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
      var constructor = bundle.JobDetail.JobType.GetConstructors().Single();
      var args = constructor.GetParameters().Select(f => services.GetService(f.ParameterType)).ToArray();
      return (IJob)constructor.Invoke(args);
    }

    public void ReturnJob(IJob job)
    {
      (job as IDisposable)?.Dispose();
    }
  }
}
