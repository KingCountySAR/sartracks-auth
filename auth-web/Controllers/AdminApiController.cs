using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SarData;
using SarData.Auth.Data;
using SarData.Auth.Models.AdminViewModels;

namespace auth_web.Controllers
{
  [Authorize]
  public class AdminApiController : Controller
  {
    private readonly ApplicationDbContext appDb;
    private readonly ILogger<AdminApiController> logger;

    public AdminApiController(ApplicationDbContext appDb, ILogger<AdminApiController> logger)
    {
      this.appDb = appDb;
      this.logger = logger;
    }

    [HttpGet("/admin/api/accounts")]
    [JsonApi]
    public async Task<ActionResult> ListUsers(Dictionary<string, int> page, Dictionary<string,string> filter, string sort)
    {
      if (!filter.ContainsKey(string.Empty) && Request.Query.TryGetValue("filter", out StringValues standaloneFilter) && standaloneFilter.Count == 1 && !string.IsNullOrWhiteSpace(standaloneFilter[0]))
      {
        filter.Add(string.Empty, standaloneFilter[0]);
      }

      page = page ?? new Dictionary<string, int>();

      page.TryAdd("size", 100);
      page.TryAdd("number", 1);

      if (page["number"] < 1) page["number"] = 1;
      if (page["size"] < 0) page["size"] = 10;

      // Add index on ClaimType? Requires updating the column
      var unfilteredQuery = (from u in appDb.Users
                             let n = appDb.UserClaims.Where(f => f.UserId == u.Id && f.ClaimType == "name").FirstOrDefault()
                             let l = appDb.UserClaims.Where(f => f.UserId == u.Id && f.ClaimType == "family_name").FirstOrDefault()
                             select new
                             {
                               _Type = "accounts",
                               Id = u.Id,
                               Created = u.Created,
                               MemberId = u.MemberId,
                               Name = n.ClaimValue,
                               LastName = l.ClaimValue,
                               Email = u.Email,
                               UserName = EF.Functions.Like(u.UserName, "@%") ? "@" : u.UserName, //.StartsWith('@') ? "@" : u.UserName,
                               IsLocked = u.LockoutEnd.HasValue,
                               LockoutEnd = u.LockoutEnd,
                               LastLogin = u.LastLogin
                             });
      var totalCount = await unfilteredQuery.CountAsync();

      var query = unfilteredQuery;
      int filteredCount = totalCount;

      if (filter.TryGetValue("", out string globalFilter) && !string.IsNullOrWhiteSpace(globalFilter))
      {
        query = query.Where(f => f.Name.Contains(globalFilter) || f.Email.Contains(globalFilter) || f.UserName.Contains(globalFilter));
        filteredCount = await query.CountAsync();
      }

      var accountList = await query
        .ApplySort(sort, "LastName")
        .Select(f => new
        {
          Id = f.Id,
          Meta = new
          {
            Created = f.Created
          },
          Attributes = new AccountModel
          {
            Email = f.Email,
            LastName = f.LastName,
            LockoutEnd = f.LockoutEnd,
            MemberId = f.MemberId,
            Name = f.Name,
            UserName = f.UserName,
            LastLogin = f.LastLogin,
            IsLocked = f.IsLocked
          }
        })
        .Skip((page["number"] - 1) * page["size"])
        .Take(page["size"])
        .ToListAsync();

      foreach (var account in accountList)
      {
        account.Attributes.UserName = account.Attributes.UserName.StartsWith('@') ? "@" : account.Attributes.UserName;
      }

      return Json(new
      {
        Meta = new { TotalRows = totalCount, FilteredRows = filteredCount },
        Data = accountList
      });
    }
  }
}