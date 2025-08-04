using Microsoft.AspNetCore.Identity;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Services;

public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<WitchCityRopeUser> _identityPasswordHasher;

    public PasswordHasher(Microsoft.AspNetCore.Identity.IPasswordHasher<WitchCityRopeUser> identityPasswordHasher)
    {
        _identityPasswordHasher = identityPasswordHasher;
    }

    public string HashPassword(string password)
    {
        // Create a dummy user for hashing (we only need the password hash)
        var dummyUser = new WitchCityRopeUser();
        return _identityPasswordHasher.HashPassword(dummyUser, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        // Create a dummy user for verification (we only need the password hash)
        var dummyUser = new WitchCityRopeUser();
        var result = _identityPasswordHasher.VerifyHashedPassword(dummyUser, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}