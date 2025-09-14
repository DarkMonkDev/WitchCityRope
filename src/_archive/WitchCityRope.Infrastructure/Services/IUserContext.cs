using System;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Services;

public interface IUserContext
{
    Task<User?> GetCurrentUserAsync();
    Guid? GetCurrentUserId();
}