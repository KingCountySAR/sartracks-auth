using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SarData.Auth.Data;
using SarData.Server.JsonApi;

namespace SarData.Auth.Controllers
{
  [Authorize]
  public class GroupsApiController : Controller
  {
    private readonly ApplicationDbContext appDb;
    private readonly ILogger<GroupsApiController> logger;
    private readonly IConfiguration configuration;

    public GroupsApiController(ApplicationDbContext appDb, ILogger<GroupsApiController> logger, IConfiguration configuration)
    {
      this.appDb = appDb;
      this.logger = logger;
      this.configuration = configuration;
    }

    //[HttpGet("/api/accounts/{userId}")]
    //[JsonApi]
    //public async Task<ActionResult> GetUser(string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    //{
    //  var f = await (from u in appDb.Users
    //                 where u.Id == userId
    //                 select new
    //                 {
    //                   _Type = "accounts",
    //                   Id = u.Id,
    //                   Created = u.Created,
    //                   MemberId = u.MemberId,
    //                   Name = u.FirstName + " " + u.LastName,
    //                   FirstName = u.FirstName,
    //                   LastName = u.LastName,
    //                   Email = u.Email,
    //                   UserName = EF.Functions.Like(u.UserName, "@%") ? "@" : u.UserName, //.StartsWith('@') ? "@" : u.UserName,
    //                   IsLocked = u.LockoutEnd.HasValue,
    //                   LockoutEnd = u.LockoutEnd,
    //                   LastLogin = u.LastLogin
    //                 }).FirstOrDefaultAsync();

    //  return Json(new
    //  {
    //    Data = new
    //    {
    //      Type = "accounts",
    //      Id = f.Id,
    //      Meta = new
    //      {
    //        Created = f.Created
    //      },
    //      Attributes = new AccountModel
    //      {
    //        Email = f.Email,
    //        LastName = f.LastName,
    //        LockoutEnd = f.LockoutEnd,
    //        MemberId = f.MemberId,
    //        Name = f.Name,
    //        UserName = f.UserName,
    //        LastLogin = f.LastLogin,
    //        IsLocked = f.IsLocked
    //      }
    //    }
    //  });
    //}

    [HttpGet("/api/groups")]
    [JsonApi]
    public async Task<ActionResult> ListGroups(Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    {
      ListQueryStrategy mods = new ListQueryBuilder("Name").Build(page, filter, sort, Request.Query);

      return Json(await mods.Run(
        from u in appDb.Roles
        select new
        {
          _Type = "groups",
          Id = u.Id,
          Name = u.Name,
          Description = u.Description
          //Created = u.Created,
          //MemberId = u.MemberId,
          //Name = u.FirstName + " " + u.LastName,
          //FirstName = u.FirstName,
          //LastName = u.LastName,
          //Email = u.Email,
          //UserName = EF.Functions.Like(u.UserName, "@%") ? "@" : u.UserName,
          //IsLocked = u.LockoutEnd.HasValue,
          //LockoutEnd = u.LockoutEnd,
          //LastLogin = u.LastLogin
        },
        globalFilter => f => f.Name.Contains(globalFilter) || f.Description.Contains(globalFilter),
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
            //  Email = f.Email,
            //  LastName = f.LastName,
            //  LockoutEnd = f.LockoutEnd,
            //  MemberId = f.MemberId,
            Name = f.Name,
            Description = f.Description
            //  UserName = f.UserName,
            //  LastLogin = f.LastLogin,
            //  IsLocked = f.IsLocked
          }
        },
        null //account => account.Attributes.UserName = account.Attributes.UserName.StartsWith('@') ? "@" : account.Attributes.UserName
        ));
    }

    //[HttpGet("/api/Accounts/{userId}/ExternalLogins")]
    //[JsonApi]
    //public async Task<ActionResult> ListUserLogins(string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    //{
    //  var oidcConfigs = JsonConvert.DeserializeObject<OidcConfig[]>(string.IsNullOrWhiteSpace(configuration["auth:external:oidc"]) ? "[]" : configuration["auth:external:oidc"]).ToDictionary(f => f.Id, f => f);
    //  ListQueryStrategy mods = new ListModifiersBuilder("ProviderDisplayName").Build(page, filter, sort, Request.Query);

    //  return Json(await mods.Run(
    //    from l in appDb.UserLogins
    //    where l.UserId == userId
    //    select l,
    //    globalFilter => f => f.ProviderDisplayName.Contains(globalFilter),
    //    f => new
    //    {
    //      Type = "externallogins",
    //      Id = f.LoginProvider + ":" + f.ProviderKey,
    //      Meta = new ExternalLoginMeta
    //      {
    //        Icon = f.LoginProvider + ":" + f.ProviderKey,
    //        Color = "b"
    //      },
    //      Attributes = new
    //      {
    //        DisplayName = f.ProviderDisplayName,
    //        Provider = f.LoginProvider
    //      },
    //      Relationships = new
    //      {
    //        Owner = new
    //        {
    //          Data = new { Type = "accounts", Id = f.UserId }
    //        }
    //      }
    //    },
    //    row =>
    //    {
    //      row.Meta.Icon = row.Attributes.Provider == "Facebook" ? "fa-facebook-square" : row.Attributes.Provider == "Google" ? "fa-google" : oidcConfigs[row.Attributes.Provider].Icon;
    //      row.Meta.Color = row.Attributes.Provider == "Facebook" ? "#3B5998" : row.Attributes.Provider == "Google" ? "#EA4335" : oidcConfigs[row.Attributes.Provider].IconColor;
    //    }
    //    ));
    //}

    //[HttpGet("/api/Accounts/{userId}/Groups")]
    //[JsonApi]
    //public async Task<ActionResult> ListUserGroups(string userId, Dictionary<string, int> page, Dictionary<string, string> filter, string sort)
    //{
    //  ListQueryStrategy strategy = new ListModifiersBuilder("DisplayName").Build(page, filter, sort, Request.Query);

    //  //var query = appDb.UserRoles.Select(f => new { User = f.User, Role = f.Role }).SelectMany(f => f.Role.Ancestors.Select(g => new { UserId = f.User.Id, IsIn = g.ParentId, BecauseOf = g.ChildId, Nested = 1 }));
    //  var query = (from m in appDb.UserRoles
    //               join link in appDb.Roles.SelectMany(f => f.Ancestors) on m.RoleId equals link.ChildId
    //               select new
    //               {
    //                 UserId = m.UserId,
    //                 IsIn = link.ParentId,
    //                 DisplayName = link.Parent.Name,
    //                 BecauseOf = m.RoleId,
    //                 BecauseName = m.Role.Name,
    //                 Nested = 1
    //               }).Union(from m in appDb.UserRoles
    //                        select new
    //                        {
    //                          UserId = m.UserId,
    //                          IsIn = m.RoleId,
    //                          DisplayName = m.Role.Name,
    //                          BecauseOf = m.RoleId,
    //                          BecauseName = m.Role.Name,
    //                          Nested = 0
    //                        })
    //                        .Where(f => f.UserId == userId)
    //                        .OrderBy(f => f.BecauseName).ThenBy(f => f.Nested).ThenBy(f => f.DisplayName);


    //  var list = await query.ToListAsync();

    //  return Json(await strategy.Run(
    //    query,
    //    q => f => f.DisplayName.Contains(q),
    //    f => new
    //    {
    //      Type = "groups",
    //      Id = f.IsIn,
    //      Attributes = new
    //      {
    //        DisplayName = f.DisplayName
    //      },
    //      Relationships = new GroupRelationshipsModel
    //      {
    //        Parent = new GroupParentModel
    //        {
    //          Data = new JsonApiResourceId { Type = "groups", Id = f.BecauseOf }
    //        }
    //      }
    //    },
    //    row =>
    //    {
    //      row.Relationships.Parent = row.Relationships.Parent.Data.Id == row.Id ? null : row.Relationships.Parent;
    //    }));
    //}

    //class GroupRelationshipsModel
    //{
    //  public GroupParentModel Parent { get; set; }
    //}

    //class GroupParentModel
    //{
    //  public JsonApiResourceId Data { get; set; }
    //}

    //class JsonApiResourceId
    //{
    //  public string Type { get; set; }
    //  public string Id { get; set; }
    //}


    //class ExternalLoginMeta
    //{
    //  public string Icon { get; set; }
    //  public string Color { get; set; }
    //}
  }
}