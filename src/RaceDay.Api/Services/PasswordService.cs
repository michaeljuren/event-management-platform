using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Thin wrapper around ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/>
/// (PBKDF2 with HMAC-SHA512, 100 000 iterations, random 128-bit salt per password).
/// </summary>
public interface IPasswordService
{
    string Hash(User user, string password);

    /// <summary>Verifies a password. When the stored hash uses outdated parameters it is upgraded in place.</summary>
    bool Verify(User user, string password);
}

public class PasswordService(IPasswordHasher<User> hasher) : IPasswordService
{
    public string Hash(User user, string password) => hasher.HashPassword(user, password);

    public bool Verify(User user, string password)
    {
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
        }

        return result != PasswordVerificationResult.Failed;
    }
}
