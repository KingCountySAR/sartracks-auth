using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quartz;
using SarData.Accounts.Quartz;
using SarData.Accounts.Quartz.Jobs;
using SarData.Accounts.Quartz.Jobs.GSuite;

namespace SarData.Accounts.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class WeatherForecastController : ControllerBase
  {
    private readonly IScheduler scheduler;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(IScheduler scheduler, ILogger<WeatherForecastController> logger)
    {
      this.scheduler = scheduler;
      _logger = logger;
    }

    [HttpGet("schedule/{username}")]
    /*[Authorize]*/
    public async Task<string> Get(string username)
    {
      var task = await OnboardMemberJob.Create(scheduler, new OnboardMemberJob.Options {
        Username = "alice.user@mydomain.org",
        PersonalEmail = "example@example.com",
        FirstName = "Alice",
        LastName = "Volunteer"
      });

      await scheduler.TriggerJob(task.Key);

      return "ok";
    }
  }
}
