using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using SarData;
using SarData.Auth.Data;
using SarData.Auth.Models;
using SarData.Auth.Models.AdminViewModels;

namespace SarData.Auth.Controllers
{
  [Authorize]
  public class ApiController : Controller
  {
    private readonly ApplicationDbContext appDb;
    private readonly ILogger<ApiController> logger;
    private readonly IConfiguration configuration;

    public ApiController(ApplicationDbContext appDb, ILogger<ApiController> logger, IConfiguration configuration)
    {
      this.appDb = appDb;
      this.logger = logger;
      this.configuration = configuration;
    }

    public static async Task<object> GetUserApplications(ApplicationDbContext appDb, string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort, IQueryCollection query)
    {
      ListQueryStrategy strategy = new ListModifiersBuilder("Name").Build(page, filter, sort, query);
      var appsQuery = appDb.Applications.Where(f => f.Organizations.Count == 0 || f.Organizations.Any(g => appDb.Users.Where(h => h.Id == userId).SelectMany(h => h.CustomOrganizations).Select(h => h.OrganizationId).Contains(g.OrganizationId)))
        .Select(a => new
        {
          _Type = "applications",
          Id = a.Id,
          Name = a.Name,
          Url = a.Url,
          Description = a.Description,
          Logo = a.Logo
        });

      return await strategy.Run(
        appsQuery,
        q => f => f.Name.Contains(q),
        f => new
        {
          Type = f._Type,
          Id = f.Id,
          //Meta = new
          //{
          //  Created = f.Created
          //},
          Attributes = new
          {
            Name = f.Name,
            Description = f.Description,
            Url = f.Url,
            Logo = f.Logo
          }
        },
        null);
    }

    [HttpGet("/api/accounts/{userId}/applications")]
    [JsonApi]
    public async Task<ActionResult> GetUserApplicationsApi(string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    {
      return Json(await GetUserApplications(appDb, userId, page, filter, sort, Request.Query));      
    }

    [HttpGet("/api/accounts/{userId}")]
    [JsonApi]
    public async Task<ActionResult> GetUser(string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    {
      var f = await (from u in appDb.Users
                     where u.Id == userId
                     select new
                     {
                       _Type = "accounts",
                       Id = u.Id,
                       Created = u.Created,
                       MemberId = u.MemberId,
                       Name = u.FirstName + " " + u.LastName,
                       FirstName = u.FirstName,
                       LastName = u.LastName,
                       Email = u.Email,
                       UserName = EF.Functions.Like(u.UserName, "@%") ? "@" : u.UserName, //.StartsWith('@') ? "@" : u.UserName,
                       IsLocked = u.LockoutEnd.HasValue,
                       LockoutEnd = u.LockoutEnd,
                       LastLogin = u.LastLogin
                     }).FirstOrDefaultAsync();

      return Json(new
      {
        Data = new
        {
          Type = "accounts",
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
        }
      });
    }

    [HttpGet("/api/accounts")]
    [JsonApi]
    public async Task<ActionResult> ListUsers(Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    {
      ListQueryStrategy mods = new ListModifiersBuilder("LastName").Build(page, filter, sort, Request.Query);
            
      return Json(await mods.Run(
        from u in appDb.Users
        select new
        {
          _Type = "accounts",
          Id = u.Id,
          Created = u.Created,
          MemberId = u.MemberId,
          Name = u.FirstName + " " + u.LastName,
          FirstName = u.FirstName,
          LastName = u.LastName,
          Email = u.Email,
          UserName = EF.Functions.Like(u.UserName, "@%") ? "@" : u.UserName,
          IsLocked = u.LockoutEnd.HasValue,
          LockoutEnd = u.LockoutEnd,
          LastLogin = u.LastLogin
        },
        globalFilter => f => f.Name.Contains(globalFilter) || f.Email.Contains(globalFilter) || f.UserName.Contains(globalFilter),
        f => new
        {
          Type = f._Type,
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
        },
        account => account.Attributes.UserName = account.Attributes.UserName.StartsWith('@') ? "@" : account.Attributes.UserName
        ));
    }
    
    [HttpGet("/api/Accounts/{userId}/ExternalLogins")]
    [JsonApi]
    public async Task<ActionResult> ListUserLogins(string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    {
      var oidcConfigs = JsonConvert.DeserializeObject<OidcConfig[]>(string.IsNullOrWhiteSpace(configuration["auth:external:oidc"]) ? "[]" : configuration["auth:external:oidc"]).ToDictionary(f => f.Id, f => f);
      ListQueryStrategy mods = new ListModifiersBuilder("ProviderDisplayName").Build(page, filter, sort, Request.Query);

      return Json(await mods.Run(
        from l in appDb.UserLogins
        where l.UserId == userId
        select l,
        globalFilter => f => f.ProviderDisplayName.Contains(globalFilter),
        f => new
        {
          Type = "externallogins",
          Id = f.LoginProvider + ":" + f.ProviderKey,
          Meta = new ExternalLoginMeta
          {
            Icon = f.LoginProvider + ":" + f.ProviderKey,
            Color = "b"
          },
          Attributes = new
          {
            DisplayName = f.ProviderDisplayName,
            Provider = f.LoginProvider
          },
          Relationships = new
          {
            Owner = new
            {
              Data = new { Type = "accounts", Id = f.UserId }
            }
          }
        },
        row =>
        {
          row.Meta.Icon = row.Attributes.Provider == "Facebook" ? "fa-facebook-square" : row.Attributes.Provider == "Google" ? "fa-google" : oidcConfigs[row.Attributes.Provider].Icon;
          row.Meta.Color = row.Attributes.Provider == "Facebook" ? "#3B5998" : row.Attributes.Provider == "Google" ? "#EA4335" : oidcConfigs[row.Attributes.Provider].IconColor;
        }
        ));
    }

    [HttpGet("/api/Accounts/{userId}/Groups")]
    [JsonApi]
    public async Task<ActionResult> ListUserGroups(string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    {
      ListQueryStrategy strategy = new ListModifiersBuilder("DisplayName").Build(page, filter, sort, Request.Query);

      //var query = appDb.UserRoles.Select(f => new { User = f.User, Role = f.Role }).SelectMany(f => f.Role.Ancestors.Select(g => new { UserId = f.User.Id, IsIn = g.ParentId, BecauseOf = g.ChildId, Nested = 1 }));
      var query = (from m in appDb.UserRoles
                   join link in appDb.Roles.SelectMany(f => f.Ancestors) on m.RoleId equals link.ChildId
                   select new
                   {
                     UserId = m.UserId,
                     IsIn = link.ParentId,
                     DisplayName = link.Parent.Name,
                     BecauseOf = m.RoleId,
                     BecauseName = m.Role.Name,
                     Nested = 1
                   }).Union(from m in appDb.UserRoles
                            select new
                            {
                              UserId = m.UserId,
                              IsIn = m.RoleId,
                              DisplayName = m.Role.Name,
                              BecauseOf = m.RoleId,
                              BecauseName = m.Role.Name,
                              Nested = 0
                            })
                            .Where(f => f.UserId == userId)
                            .OrderBy(f => f.BecauseName).ThenBy(f => f.Nested).ThenBy(f => f.DisplayName);


      var list = await query.ToListAsync();

      return Json(await strategy.Run(
        query,
        q => f => f.DisplayName.Contains(q),
        f => new
        {
          Type = "groups",
          Id = f.IsIn,
          Attributes = new
          {
            DisplayName = f.DisplayName
          },
          Relationships = new GroupRelationshipsModel
          {
            Parent = new GroupParentModel
            {
              Data = new JsonApiResourceId { Type = "groups", Id = f.BecauseOf }
            }
          }
        },
        row =>
        {
          row.Relationships.Parent = row.Relationships.Parent.Data.Id == row.Id ? null : row.Relationships.Parent;
        }));
    }

    class GroupRelationshipsModel
    {
      public GroupParentModel Parent { get; set; }
    }

    class GroupParentModel
    {
      public JsonApiResourceId Data { get; set; }
    }

    class JsonApiResourceId
    {
      public string Type { get; set; }
      public string Id { get; set; }
    }


    class ExternalLoginMeta
    {
      public string Icon { get; set; }
      public string Color { get; set; }
    }

    class ListModifiersBuilder
    {
      public int DefaultPageSize { get; set; } = 10;

      public string DefaultSort { get; private set; }

      public ListModifiersBuilder(string defaultSort)
      {
        DefaultSort = defaultSort;
      }

      public ListQueryStrategy Build(Dictionary<string, int> page, Dictionary<string, string> filter, string sort, IQueryCollection query)
      {
        if (!filter.ContainsKey(string.Empty) && query != null && query.TryGetValue("filter", out StringValues standaloneFilter) && standaloneFilter.Count == 1 && !string.IsNullOrWhiteSpace(standaloneFilter[0]))
        {
          filter.Add(string.Empty, standaloneFilter[0]);
        }

        page = page ?? new Dictionary<string, int>();

        page.TryAdd("size", 0);
        page.TryAdd("number", 1);

        if (page["number"] < 1) page["number"] = 1;
        if (page["size"] < 0) page["size"] = 10;

        return new ListQueryStrategy(page, filter, sort, DefaultSort);
      }
    }

    class ListQueryStrategy
    {
      private readonly Dictionary<string, int> page;
      private readonly Dictionary<string, string> filter;
      private readonly string sort;
      private readonly string defaultSort;

      public ListQueryStrategy(Dictionary<string, int> page, Dictionary<string, string> filter, string sort, string defaultSort)
      {
        this.page = page;
        this.filter = filter;
        this.sort = sort;
        this.defaultSort = defaultSort;
      }

      public async Task<object> Run<Q,P>(
        IQueryable<Q> unfilteredQuery,
        Func<string, Expression<Func<Q, bool>>> searchBuilder,
        Expression<Func<Q, P>> projection,
        Action<P> localProcessor
        )
      {
        var query = ApplyFilters(unfilteredQuery, searchBuilder);

        var accountList = await ApplyPaging(ApplySort(query)).Select(projection).ToListAsync();

        var totalCount = await unfilteredQuery.CountAsync();
        int filteredCount = totalCount;
        if (query != unfilteredQuery)
        {
          filteredCount = await query.CountAsync();
        }

        if (localProcessor != null)
        {
          foreach (var row in accountList)
          {
            localProcessor(row);
          }
        }

        return new
        {
          Meta = new { TotalRows = totalCount, FilteredRows = filteredCount },
          Data = accountList
        };
      }

      private IQueryable<T> ApplyFilters<T>(IQueryable<T> original, Func<string, Expression<Func<T,bool>>> searchBuilder)
      {
        if (filter.TryGetValue("", out string globalFilter) && !string.IsNullOrWhiteSpace(globalFilter))
        {
          return original.Where(searchBuilder(globalFilter));
        }
        return original;
      }

      private IOrderedQueryable<T> ApplySort<T>(IQueryable<T> original)
      {
        return original.ApplySort(sort, defaultSort);
      }

      private IQueryable<T> ApplyPaging<T>(IQueryable<T> query)
      {
        if (page["size"] > 0)
        {
          if (page["number"] > 1)
          {
            query = query.Skip((page["number"] - 1) * page["size"]);
          }
          query = query.Take(page["size"]);
        }
        return query;
      }
    }
  }
}