using System.Collections.Concurrent;
using WitchCityRope.Core.Entities;
using WitchCityRope.Tests.Common.Interfaces;

namespace WitchCityRope.Tests.Common.TestDoubles
{
    /// <summary>
    /// In-memory implementation of IUserRepository for testing
    /// </summary>
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new();
        private readonly ConcurrentDictionary<string, User> _usersByEmail = new();
        private readonly ConcurrentDictionary<string, User> _usersBySceneName = new();
        private readonly ConcurrentDictionary<string, Interfaces.RefreshToken> _refreshTokens = new();

        public bool SimulateFailure { get; set; }
        public int DelayMs { get; set; }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _users.TryGetValue(id, out var user) ? user : null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _usersByEmail.TryGetValue(email.ToLowerInvariant(), out var user) ? user : null;
        }

        public async Task<User?> GetBySceneNameAsync(string sceneName)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _usersBySceneName.TryGetValue(sceneName.ToLowerInvariant(), out var user) ? user : null;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _users.ContainsKey(id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _usersByEmail.ContainsKey(email.ToLowerInvariant());
        }

        public async Task<bool> SceneNameExistsAsync(string sceneName)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _usersBySceneName.ContainsKey(sceneName.ToLowerInvariant());
        }

        public async Task<User> CreateAsync(User user)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            // Note: User entity properties like Id, CreatedAt, UpdatedAt are read-only
            // and should be set through the entity's constructor or methods

            if (!_users.TryAdd(user.Id, user))
                throw new InvalidOperationException("User with this ID already exists");

            _usersByEmail[user.Email.Value.ToLowerInvariant()] = user;
            _usersBySceneName[user.SceneName.Value.ToLowerInvariant()] = user;

            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            if (!_users.TryGetValue(user.Id, out var existingUser))
                throw new InvalidOperationException("User not found");

            // Update indexes if email or scene name changed
            if (!string.Equals(existingUser.Email.Value, user.Email.Value, StringComparison.OrdinalIgnoreCase))
            {
                _usersByEmail.TryRemove(existingUser.Email.Value.ToLowerInvariant(), out _);
                _usersByEmail[user.Email.Value.ToLowerInvariant()] = user;
            }

            if (!string.Equals(existingUser.SceneName.Value, user.SceneName.Value, StringComparison.OrdinalIgnoreCase))
            {
                _usersBySceneName.TryRemove(existingUser.SceneName.Value.ToLowerInvariant(), out _);
                _usersBySceneName[user.SceneName.Value.ToLowerInvariant()] = user;
            }

            // Note: UpdatedAt is read-only and should be set through entity methods
            _users[user.Id] = user;

            return user;
        }

        public async Task DeleteAsync(Guid id)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            if (_users.TryRemove(id, out var user))
            {
                _usersByEmail.TryRemove(user.Email.Value.ToLowerInvariant(), out _);
                _usersBySceneName.TryRemove(user.SceneName.Value.ToLowerInvariant(), out _);

                // Remove user's refresh tokens
                var userTokens = _refreshTokens.Where(kvp => kvp.Value.UserId == id).ToList();
                foreach (var token in userTokens)
                {
                    _refreshTokens.TryRemove(token.Key, out _);
                }
            }
        }

        public async Task<List<User>> GetAllAsync()
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _users.Values.ToList();
        }

        public async Task<Interfaces.RefreshToken?> GetRefreshTokenAsync(string token)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            return _refreshTokens.TryGetValue(token, out var refreshToken) ? refreshToken : null;
        }

        public async Task SaveRefreshTokenAsync(Interfaces.RefreshToken refreshToken)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            _refreshTokens[refreshToken.Token] = refreshToken;
        }

        public async Task DeleteRefreshTokenAsync(string token)
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            _refreshTokens.TryRemove(token, out _);
        }

        public async Task DeleteExpiredRefreshTokensAsync()
        {
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (SimulateFailure) throw new InvalidOperationException("Repository failure");

            var expiredTokens = _refreshTokens
                .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _refreshTokens.TryRemove(token, out _);
            }
        }

        public void Clear()
        {
            _users.Clear();
            _usersByEmail.Clear();
            _usersBySceneName.Clear();
            _refreshTokens.Clear();
        }

        public int UserCount => _users.Count;
        public int RefreshTokenCount => _refreshTokens.Count;
    }
}