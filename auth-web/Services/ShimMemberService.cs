using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SarData.Auth.Data;
using SarData.Auth.Models;

namespace SarData.Auth.Services
{
  public class ShimMemberService : IRemoteMembersService
  {
    private readonly MembershipShimDbContext db;

    Expression<Func<MemberContactShimRow, RemoteMember>> remoteMemberTransform = f => new RemoteMember
    {
      Id = f.Member.Id.ToString(),
      FirstName = f.Member.FirstName,
      LastName = f.Member.LastName,
      IsActive = f.Member.Memberships.Any(g => g.EndTime == null && g.Status.IsActive == true),
      PrimaryEmail = f.Member.Contacts.Where(g => g.Type == "email").OrderBy(g => g.Priority).Select(g => g.Value).FirstOrDefault(),
      PrimaryPhone = f.Member.Contacts.Where(g => g.Type == "phone").OrderBy(g => g.Priority).Select(g => g.Value).FirstOrDefault()
    };


    public ShimMemberService(MembershipShimDbContext db)
    {
      this.db = db;
    }

    public async Task<List<RemoteMember>> FindByEmail(string email)
    {
      return await db.Contacts.Where(f => f.Value == email && f.Type == "email").Select(remoteMemberTransform).ToListAsync();
    }

    public async Task<List<RemoteMember>> FindByPhone(string phone)
    {
      return await db.Contacts.Where(f => f.Value == phone && f.Type == "phone").Select(remoteMemberTransform).ToListAsync();
    }

    public Task<RemoteMember> GetMember(string id)
    {
      Guid gid = Guid.Parse(id);
      return db.Members.Where(f => f.Id == gid).Select(f => new RemoteMember
      {
        Id = f.Id.ToString(),
        FirstName = f.FirstName,
        LastName = f.LastName,
        IsActive = f.Memberships.Any(g => g.EndTime == null && g.Status.IsActive == true),
        PrimaryEmail = f.Contacts.Where(g => g.Type == "email").OrderBy(g => g.Priority).Select(g => g.Value).FirstOrDefault(),
        PrimaryPhone = f.Contacts.Where(g => g.Type == "phone").OrderBy(g => g.Priority).Select(g => g.Value).FirstOrDefault()
      }).FirstOrDefaultAsync();
    }
  }
}
