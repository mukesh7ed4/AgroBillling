// ================================================
//  AgroBilling.DAL / Repositories / ShopRepository.cs
//  ✅ FIXED — PostgreSQL compatible (removed SQL Server syntax)
// ================================================

using System.Collections.Generic;
using AgroBilling.DAL.Context;
using AgroBilling.DAL.Models;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBilling.DAL.Repositories
{
    public class ShopRepository : GenericRepository<Shop>, IShopRepository
    {
        public ShopRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<Shop?> GetByEmailAsync(string email) =>
            await _context.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Email == email);

        public async Task<IEnumerable<Shop>> GetAllWithSubscriptionsAsync() =>
            await _context.Shops
                .AsNoTracking()
                .Include(s => s.ShopSubscriptions.Where(ss => ss.IsActive == true))
                    .ThenInclude(ss => ss.Plan)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<(IReadOnlyList<Shop> Items, int TotalCount)>
            GetPagedWithSubscriptionsAsync(string? search, int page, int pageSize)
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

        // ✅ FIXED: PostgreSQL syntax — no SQL Server hints
        public async Task<int> GetNextBillNumberAsync(int shopId)
        {
            // PostgreSQL: FOR UPDATE row lock
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE "Shops"
                SET "CurrentBillSequence" = "CurrentBillSequence" + 1
                WHERE "ShopID" = {shopId}
                """);

            var next = await _context.Shops
                .AsNoTracking()
                .Where(s => s.ShopId == shopId)
                .Select(s => s.BillStartNumber + s.CurrentBillSequence - 1)
                .FirstOrDefaultAsync();

            return next <= 0 ? 1 : next;
        }
    }
}