using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        /// <summary>Looks a token up by its hash, user included.</summary>
        Task<RefreshToken?> GetByHash(string tokenHash);

        /// <summary>Every token of a user that can still be spent.</summary>
        Task<List<RefreshToken>> GetActiveTokensForUser(int userID);

        Task AddAndSave(RefreshToken token);
        Task SaveChanges();

        /// <summary>Drops rows that expired (or were revoked) long enough ago to be useless.</summary>
        Task<int> DeleteExpired(TimeSpan retention);
    }
}
