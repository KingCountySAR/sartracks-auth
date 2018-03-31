using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SarData.Auth.Models;

namespace SarData.Auth.Identity
{
  /// <summary>Password Hasher that knows how to hash to the SHA1 implementation used by previous database.</summary>
  public class LegacyPasswordHasher : PasswordHasher<ApplicationUser>
  {
    public const int PasswordSaltLength = 24;

    public LegacyPasswordHasher(IOptions<PasswordHasherOptions> options) : base(options)
    {
    }

    public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
      if (hashedPassword.Length == 52)
      {
        var salt = hashedPassword.Substring(0, PasswordSaltLength);
        var hashed = HashPassword(providedPassword, salt);
        return string.Equals(hashedPassword.Substring(PasswordSaltLength), hashed) ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Failed;
      }
      return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }

    public static string HashPassword(string password, string salt)
    {
      byte[] bytes = Encoding.Unicode.GetBytes(password);
      byte[] src = Convert.FromBase64String(salt);
      byte[] dst = new byte[src.Length + bytes.Length];
      Buffer.BlockCopy(src, 0, dst, 0, src.Length);
      Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);
      HashAlgorithm algorithm = (HashAlgorithm)CryptoConfig.CreateFromName("SHA1");
      byte[] inArray = algorithm.ComputeHash(dst);
      return Convert.ToBase64String(inArray);
    }
  }
}
