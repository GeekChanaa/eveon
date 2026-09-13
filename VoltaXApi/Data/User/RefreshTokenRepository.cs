using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(VoltaXApiDbContext context) : base(context)
        {
        }

        public async Task<RefreshToken?> GetByHash(string tokenHash)
        {
            return await this._context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task<List<RefreshToken>> GetActiveTokensForUser(int userID)
        {
            var now = DateTime.UtcNow;
            return await this._context.RefreshTokens
                .Where(t => t.UserID == userID && t.RevokedAt == null && t.ExpiresAt > now)
                .ToListAsync();
        }

        public async Task AddAndSave(RefreshToken token)
        {
            await this._context.RefreshTokens.AddAsync(token);
            await this._context.SaveChangesAsync();
        }

        public Task SaveChanges()
        {
            return this._context.SaveChangesAsync();
        }

        public async Task<int> DeleteExpired(TimeSpan retention)
        {
            var cutoff = DateTime.UtcNow - retention;
            var stale = await this._context.RefreshTokens
                .Where(t => t.ExpiresAt < cutoff || (t.RevokedAt != null && t.RevokedAt < cutoff))
                .ToListAsync();

            if (stale.Count == 0)
                return 0;

            this._context.RefreshTokens.RemoveRange(stale);
            await this._context.SaveChangesAsync();

            return stale.Count;
        }
    }
}
