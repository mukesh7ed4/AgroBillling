// ================================================
//  AgroBilling.DAL / Repositories / ShopRepository.cs
// ================================================

using System.Collections.Generic;
using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class ShopRepository : GenericRepository<Shop>, IShopRepository
    {
        public ShopRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<Shop?> GetByEmailAsync(string email) =>
            await _context.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Email == email);

        public async Task<IEnumerable<Shop>> GetAllWithSubscriptionsAsync() =>
            await _context.Shops
                .AsNoTracking()
                .Include(s => s.ShopSubscriptions.Where(ss => ss.IsActive == true))
                    .ThenInclude(ss => ss.Plan)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<(IReadOnlyList<Shop> Items, int TotalCount)> GetPagedWithSubscriptionsAsync(
            string? search, int page, int pageSize)
        {
            var query = _context.Shops.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x =>
                    x.ShopName.ToLower().Contains(s) ||
                    x.OwnerName.ToLower().Contains(s) ||
                    (x.City != null && x.City.ToLower().Contains(s)) ||
                    x.MobileNumber.Contains(s));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.ShopSubscriptions.Where(ss => ss.IsActive == true))
                    .ThenInclude(ss => ss.Plan)
                .ToListAsync();

            return (items, total);
        }

        /// <summary>
        /// Atomically increments sequence and returns the next bill number (no SaveChanges on Shops).
        /// Uses row lock; runs inside the caller's EF transaction when one is active.
        /// </summary>
        public async Task<int> GetNextBillNumberAsync(int shopId)
        {
            var rows = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE dbo.Shops WITH (UPDLOCK, ROWLOCK)
                SET CurrentBillSequence = CurrentBillSequence + 1
                WHERE ShopID = {shopId};
                """);

            if (rows == 0)
                return 1;

            var next = await _context.Shops
                .AsNoTracking()
                .Where(s => s.ShopId == shopId)
                .Select(s => s.BillStartNumber + s.CurrentBillSequence - 1)
                .FirstOrDefaultAsync();

            return next <= 0 ? 1 : next;
        }
    }
}
