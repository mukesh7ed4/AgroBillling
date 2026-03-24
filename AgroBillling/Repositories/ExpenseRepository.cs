// ================================================
//  AgroBilling.DAL / Repositories / ExpenseRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
    {
        public ExpenseRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<IEnumerable<Expense>> GetByShopIdAsync(int shopId, int year, int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate   = startDate.AddMonths(1).AddDays(-1);

            return await _context.Expenses
                .AsNoTracking()
                .Include(e => e.Category)
                .Where(e => e.ShopId == shopId &&
                            e.ExpenseDate >= startDate &&
                            e.ExpenseDate <= endDate)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<Expense> Items, int TotalCount, decimal MonthTotal)> GetPagedForMonthAsync(
            int shopId, int year, int month, int page, int pageSize)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate   = startDate.AddMonths(1).AddDays(-1);

            var query = _context.Expenses
                .AsNoTracking()
                .Include(e => e.Category)
                .Where(e => e.ShopId == shopId &&
                            e.ExpenseDate >= startDate &&
                            e.ExpenseDate <= endDate);

            var total = await query.CountAsync();
            var monthTotal = await query.SumAsync(e => e.Amount);
            var items = await query
                .OrderByDescending(e => e.ExpenseDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total, monthTotal);
        }

        public async Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync(int? shopId = null)
        {
            // Return system categories + shop-specific categories
            return await _context.ExpenseCategories
                .AsNoTracking()
                .Where(c => c.IsActive == true &&
                           (c.ShopId == null || c.ShopId == shopId))
                .OrderBy(c => c.IsSystem)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();
        }
    }
}
