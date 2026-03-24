// ================================================
//  AgroBilling.DAL / Repositories / ReportRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AgroBillingDbContext _context;

        public ReportRepository(AgroBillingDbContext context)
        {
            _context = context;
        }

        public async Task<MonthlyDashboardDto> GetMonthlyDashboardAsync(int shopId, int year, int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate   = startDate.AddMonths(1).AddDays(-1);

            // Sequential: same DbContext must not run multiple queries concurrently (EF Core).
            var bills = await _context.Bills
                .AsNoTracking()
                .Where(b => b.ShopId == shopId &&
                            b.IsReturn == false &&
                            b.BillDate >= startDate &&
                            b.BillDate <= endDate)
                .ToListAsync();

            var expenses = await _context.Expenses
                .AsNoTracking()
                .Include(e => e.Category)
                .Where(e => e.ShopId == shopId &&
                            e.ExpenseDate >= startDate &&
                            e.ExpenseDate <= endDate)
                .ToListAsync();

            var purchases = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(p => p.ShopId == shopId &&
                            p.PurchaseDate >= startDate &&
                            p.PurchaseDate <= endDate)
                .ToListAsync();

            var supplierPayments = await _context.SupplierPayments
                .AsNoTracking()
                .Where(p => p.ShopId == shopId &&
                            p.PaymentDate >= startDate &&
                            p.PaymentDate <= endDate)
                .ToListAsync();

            var topProducts = await _context.BillItems
                .AsNoTracking()
                .Where(bi => bi.Bill.ShopId == shopId &&
                             bi.Bill.IsReturn == false &&
                             bi.Bill.BillDate >= startDate &&
                             bi.Bill.BillDate <= endDate)
                .GroupBy(bi => bi.ProductName)
                .Select(g => new TopProductDto
                {
                    ProductName = g.Key,
                    TotalQty    = g.Sum(bi => bi.Quantity),
                    TotalAmount = g.Sum(bi => bi.TotalAmount)
                })
                .OrderByDescending(p => p.TotalAmount)
                .Take(5)
                .ToListAsync();

            var salesSummary = new ShopDashboardStatsDto
            {
                TotalBills       = bills.Count,
                TotalSales       = bills.Sum(b => b.TotalAmount),
                TotalCollected   = bills.Sum(b => b.AmountPaid),
                TotalPending     = bills.Sum(b => b.AmountPending ?? 0),
                PaidBills        = bills.Count(b => b.PaymentStatus == "PAID"),
                PartialBills     = bills.Count(b => b.PaymentStatus == "PARTIAL"),
                PendingBills     = bills.Count(b => b.PaymentStatus == "PENDING")
            };

            var expenseBreakdown = expenses
                .GroupBy(e => e.Category?.CategoryName ?? "Other")
                .Select(g => new ExpenseSummaryDto
                {
                    CategoryName = g.Key,
                    TotalExpense = g.Sum(e => e.Amount)
                })
                .OrderByDescending(e => e.TotalExpense)
                .ToList();

            return new MonthlyDashboardDto
            {
                SalesSummary      = salesSummary,
                ExpenseBreakdown  = expenseBreakdown,
                TotalExpenses     = expenses.Sum(e => e.Amount),
                TotalPurchased    = purchases.Sum(p => p.NetPayable),
                PaidToSuppliers   = supplierPayments.Sum(p => p.Amount),
                TopProducts       = topProducts
            };
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var in7Days = today.AddDays(7);

            // All active subscriptions with shop info
            var activeSubs = await _context.ShopSubscriptions
                .AsNoTracking()
                .Include(s => s.Shop)
                .Include(s => s.Plan)
                .Where(s => s.IsActive == true && s.Shop.IsActive == true)
                .ToListAsync();

            var expiringSoon = activeSubs
                .Where(s => s.EndDate >= today && s.EndDate <= in7Days)
                .Select(s => new ShopAlertDto
                {
                    ShopId       = s.ShopId,
                    ShopName     = s.Shop.ShopName,
                    OwnerName    = s.Shop.OwnerName,
                    MobileNumber = s.Shop.MobileNumber,
                    EndDate      = s.EndDate,
                    DaysLeft     = s.EndDate.DayNumber - today.DayNumber
                })
                .OrderBy(s => s.DaysLeft)
                .ToList();

            var expired = activeSubs
                .Where(s => s.EndDate < today)
                .Select(s => new ShopAlertDto
                {
                    ShopId       = s.ShopId,
                    ShopName     = s.Shop.ShopName,
                    OwnerName    = s.Shop.OwnerName,
                    MobileNumber = s.Shop.MobileNumber,
                    EndDate      = s.EndDate,
                    DaysLeft     = s.EndDate.DayNumber - today.DayNumber // negative
                })
                .OrderBy(s => s.EndDate)
                .ToList();

            var allShops = activeSubs
                .Select(s => new ShopSummaryDto
                {
                    ShopId       = s.ShopId,
                    ShopName     = s.Shop.ShopName,
                    OwnerName    = s.Shop.OwnerName,
                    MobileNumber = s.Shop.MobileNumber,
                    City         = s.Shop.City,
                    StartDate    = s.StartDate,
                    EndDate      = s.EndDate,
                    DaysLeft     = s.EndDate.DayNumber - today.DayNumber,
                    PlanName     = s.Plan?.PlanName ?? "—",
                    IsActive     = s.Shop.IsActive
                })
                .OrderBy(s => s.EndDate)
                .ToList();

            return new AdminDashboardDto
            {
                ExpiringSoon = expiringSoon,
                Expired      = expired,
                AllShops     = allShops
            };
        }
    }
}
