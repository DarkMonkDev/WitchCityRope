using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Services;

public interface IUserRepository
{
    Task<WitchCityRopeUser?> GetByIdAsync(Guid id);
    Task<WitchCityRopeUser?> GetByEmailAsync(string email);
    Task<bool> ExistsAsync(Guid id);
    Task AddAsync(WitchCityRopeUser user);
    Task UpdateAsync(WitchCityRopeUser user);
    Task DeleteAsync(WitchCityRopeUser user);
}