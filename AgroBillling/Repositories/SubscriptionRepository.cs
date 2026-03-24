// ================================================
//  AgroBilling.DAL / Repositories / SubscriptionRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class SubscriptionRepository : GenericRepository<ShopSubscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<ShopSubscription?> GetActiveByShopIdAsync(int shopId) =>
            await _context.ShopSubscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.ShopId == shopId && s.IsActive == true);

        public async Task<IEnumerable<ShopSubscription>> GetExpiringSoonAsync(int days)
        {
            var cutoff = DateOnly.FromDateTime(DateTime.Now.AddDays(days));
            var today  = DateOnly.FromDateTime(DateTime.Now);
            return await _context.ShopSubscriptions
                .AsNoTracking()
                .Include(s => s.Shop)
                .Where(s => s.IsActive == true && s.EndDate >= today && s.EndDate <= cutoff)
                .OrderBy(s => s.EndDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ShopSubscription>> GetExpiredAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _context.ShopSubscriptions
                .AsNoTracking()
                .Include(s => s.Shop)
                .Where(s => s.IsActive == true && s.EndDate < today)
                .OrderBy(s => s.EndDate)
                .ToListAsync();
        }

        public async Task DeactivateAllByShopIdAsync(int shopId)
        {
            var subs = await _context.ShopSubscriptions
                .Where(s => s.ShopId == shopId && s.IsActive == true)
                .ToListAsync();
            subs.ForEach(s => s.IsActive = false);
            await _context.SaveChangesAsync();
        }
    }
}
