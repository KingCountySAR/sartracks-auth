using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SarData.Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Identity
{
  public class ApplicationRoleManager : RoleManager<ApplicationRole>
  {
    public ApplicationRoleManager(IRoleStore<ApplicationRole> store, IEnumerable<IRoleValidator<ApplicationRole>> roleValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, ILogger<RoleManager<ApplicationRole>> logger)
      : base(store, roleValidators, keyNormalizer, errors, logger)
    {
    }

    public async Task RebuildInheritance()
    {
      List<RoleRoleMembership> cleanAndSort(IEnumerable<RoleRoleMembership> l) => l
        .GroupBy(f => new { f.ParentId, f.ChildId })
        .Select(f => new RoleRoleMembership { ParentId = f.Key.ParentId, ChildId = f.Key.ChildId, IsDirect = f.Any(g => g.IsDirect) })
        .OrderBy(f => f.ParentId).ThenBy(f => f.ChildId)
        .ToList();

      var links = await Roles.AsQueryable().Include(f => f.Ancestors)
        .ToDictionaryAsync(f => f.Id, f => f.Ancestors.Where(g => g.IsDirect).ToList());

      var computed = cleanAndSort(links.SelectMany(f => Flatten(f.Key, f.Value, links)));
      var roles = await Roles.AsQueryable().Include(f => f.Ancestors).ToDictionaryAsync(f => f.Id, f => f);
      var existing = cleanAndSort(roles.Values.SelectMany(f => f.Ancestors));

      List<string> updatedIds = new List<string>();
      int i = 0;
      int j = 0;
      for (; i < computed.Count && j < existing.Count; i++, j++)
      {
        if (computed[i].ParentId != existing[j].ParentId || computed[i].ChildId != existing[j].ChildId)
        {
          if (string.Compare(computed[i].ParentId, existing[j].ParentId) < 0 || string.Compare(computed[i].ChildId, existing[j].ChildId) < 0)
          {
            InsertRelation(computed[i], roles, updatedIds);
            j--;
          }
          else if (string.Compare(computed[i].ParentId, existing[j].ParentId) > 0 || string.Compare(computed[i].ChildId, existing[j].ChildId) > 0)
          {
            RemoveRelation(existing[j], roles, updatedIds);
            i--;
          }
        }
        else if (computed[i].IsDirect != existing[j].IsDirect)
        {
         // Console.WriteLine($"{computed[i]}\t//  {existing[j]}  ALTER");
          roles[computed[i].ChildId].Ancestors.Single(f => f.ParentId == computed[i].ParentId).IsDirect = computed[i].IsDirect;
          updatedIds.Add(computed[i].ChildId);
        }
        //else
        //{
        //  Console.WriteLine($"{computed[i]}\t//  {existing[j]}");
        //}
      }
      for (; i < computed.Count; i++)
      {
        InsertRelation(computed[i], roles, updatedIds);
      }
      for (; j < existing.Count; j++)
      {
        RemoveRelation(existing[j], roles, updatedIds);
      }

      foreach (var id in updatedIds)
      {
        await UpdateAsync(roles[id]);
      }
    }

    private static void RemoveRelation(RoleRoleMembership existing, Dictionary<string, ApplicationRole> roles, List<string> updatedIds)
    {
     // Console.WriteLine($"DELETE  \t // {existing}");
      var victim = roles[existing.ChildId].Ancestors.Single(f => f.ParentId == existing.ParentId);
      roles[existing.ChildId].Ancestors.Remove(victim);
      updatedIds.Add(existing.ChildId);
    }

    private static void InsertRelation(RoleRoleMembership computed, Dictionary<string, ApplicationRole> roles, List<string> updatedIds)
    {
     // Console.WriteLine($"{computed}\t  INSERT");
      roles[computed.ChildId].Ancestors.Add(computed);
      updatedIds.Add(computed.ChildId);
    }

    private IEnumerable<RoleRoleMembership> Flatten(string child, IEnumerable<RoleRoleMembership> parents, Dictionary<string, List<RoleRoleMembership>> dictionary)
    {
      var result = parents
        .SelectMany(f => Flatten(
          child,
          dictionary[f.ParentId]
            .Select(g => new RoleRoleMembership
            {
              ChildId = child,
              ParentId = g.ParentId,
              IsDirect = false
            }),
          dictionary
      )).Concat(parents);
      return result;
    }
  }
}
