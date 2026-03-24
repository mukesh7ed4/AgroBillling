// ================================================
//  AgroBilling.DAL / Repositories / BillRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class BillRepository : GenericRepository<Bill>, IBillRepository
    {
        public BillRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<IEnumerable<Bill>> GetByShopIdAsync(
            int shopId, string? search = null, string? status = null,
            int page = 1, int pageSize = 20)
        {
            var query = _context.Bills
                .AsNoTracking()
                .Where(b => b.ShopId == shopId && b.IsReturn == false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(b =>
                    b.BillNumber.ToLower().Contains(search) ||
                    _context.Customers.Any(c => c.CustomerId == b.CustomerId &&
                        (c.FullName.ToLower().Contains(search) || c.MobileNumber.Contains(search))));
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(b => b.PaymentStatus == status);

            return await query
                .OrderByDescending(b => b.BillDate)
                .ThenByDescending(b => b.BillId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new Bill
                {
                    BillId         = b.BillId,
                    ShopId         = b.ShopId,
                    CustomerId     = b.CustomerId,
                    BillNumber     = b.BillNumber,
                    BillDate       = b.BillDate,
                    SubTotal       = b.SubTotal,
                    DiscountAmount = b.DiscountAmount,
                    Gstpercent     = b.Gstpercent,
                    Gstamount      = b.Gstamount,
                    TotalAmount    = b.TotalAmount,
                    AmountPaid     = b.AmountPaid,
                    AmountPending  = b.AmountPending,
                    PaymentStatus  = b.PaymentStatus,
                    Notes          = b.Notes,
                    IsReturn       = b.IsReturn,
                    OriginalBillId = b.OriginalBillId,
                    CreatedAt      = b.CreatedAt,
                    Customer = new Customer
                    {
                        CustomerId   = b.Customer.CustomerId,
                        FullName     = b.Customer.FullName,
                        MobileNumber = b.Customer.MobileNumber
                    }
                })
                .ToListAsync();
        }

        public async Task<int> GetCountAsync(int shopId, string? status = null)
        {
            var query = _context.Bills.AsNoTracking()
                .Where(b => b.ShopId == shopId && b.IsReturn == false);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(b => b.PaymentStatus == status);
            return await query.CountAsync();
        }

        public async Task<Bill?> GetWithDetailsAsync(int billId) =>
            await _context.Bills
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Customer)
                .Include(b => b.BillItems)
                .Include(b => b.BillPayments)
                .FirstOrDefaultAsync(b => b.BillId == billId);

        public async Task<IEnumerable<Bill>> GetByCustomerIdAsync(int customerId) =>
            await _context.Bills
                .AsNoTracking()
                .Include(b => b.BillItems)
                .Include(b => b.BillPayments)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

        public async Task AddPaymentAsync(BillPayment payment)
        {
            await _context.BillPayments.AddAsync(payment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePaymentStatusAsync(int billId)
        {
            var bill = await _context.Bills
                .Include(b => b.BillPayments)
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (bill == null) return;

            var totalPaid = bill.BillPayments.Sum(p => p.Amount);
            bill.AmountPaid = totalPaid;
            bill.PaymentStatus =
                totalPaid >= bill.TotalAmount ? "PAID" :
                totalPaid > 0                ? "PARTIAL" : "PENDING";

            await _context.SaveChangesAsync();
        }
    }
}
