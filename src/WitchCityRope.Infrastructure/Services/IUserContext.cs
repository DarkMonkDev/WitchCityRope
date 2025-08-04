using System;
using System.Threading.Tasks;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Infrastructure.Services;

public interface IUserContext
{
    Task<WitchCityRopeUser?> GetCurrentUserAsync();
    Guid? GetCurrentUserId();
}