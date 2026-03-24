// ================================================
//  AgroBilling.DAL / Repositories / CustomerRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<IEnumerable<Customer>> GetByShopIdAsync(int shopId, string? search = null)
        {
            var query = _context.Customers
                .AsNoTracking()
                .Where(c => c.ShopId == shopId && c.IsActive == true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.FullName.ToLower().Contains(search) ||
                    c.MobileNumber.Contains(search) ||
                    (c.Village != null && c.Village.ToLower().Contains(search)));
            }

            return await query.OrderBy(c => c.FullName).ToListAsync();
        }

        public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedByShopIdAsync(
            int shopId, string? search, int page, int pageSize)
        {
            var query = _context.Customers
                .AsNoTracking()
                .Where(c => c.ShopId == shopId && c.IsActive == true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.FullName.ToLower().Contains(search) ||
                    c.MobileNumber.Contains(search) ||
                    (c.Village != null && c.Village.ToLower().Contains(search)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<Customer?> GetByMobileAsync(int shopId, string mobile) =>
            await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ShopId == shopId && c.MobileNumber == mobile);

        public async Task<CustomerLedgerDto> GetLedgerAsync(
            int customerId, int billsTake = 50, int paymentsTake = 100, int creditsTake = 50)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
            if (customer == null) return new CustomerLedgerDto();

            var billsTotalCount = await _context.Bills.AsNoTracking()
                .CountAsync(b => b.CustomerId == customerId);
            var paymentsTotalCount = await _context.BillPayments.AsNoTracking()
                .CountAsync(p => p.CustomerId == customerId);
            var creditNotesTotalCount = await _context.CreditNotes.AsNoTracking()
                .CountAsync(cn => cn.CustomerId == customerId);

            var ledgerTotalBilled = billsTotalCount == 0
                ? 0m
                : await _context.Bills.AsNoTracking()
                    .Where(b => b.CustomerId == customerId)
                    .SumAsync(b => b.TotalAmount);
            var ledgerTotalPaidOnBills = billsTotalCount == 0
                ? 0m
                : await _context.Bills.AsNoTracking()
                    .Where(b => b.CustomerId == customerId)
                    .SumAsync(b => b.AmountPaid);

            var bills = await _context.Bills
                .AsNoTracking()
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BillDate)
                .ThenByDescending(b => b.BillId)
                .Take(billsTake)
                .Select(b => new Bill
                {
                    BillId         = b.BillId,
                    CustomerId     = b.CustomerId,
                    BillNumber     = b.BillNumber,
                    BillDate       = b.BillDate,
                    TotalAmount    = b.TotalAmount,
                    AmountPaid     = b.AmountPaid,
                    AmountPending  = b.AmountPending,
                    PaymentStatus  = b.PaymentStatus
                })
                .ToListAsync();

            var payments = await _context.BillPayments
                .AsNoTracking()
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .Take(paymentsTake)
                .ToListAsync();

            var creditNotes = await (
                from cn in _context.CreditNotes.AsNoTracking()
                where cn.CustomerId == customerId
                join b in _context.Bills.AsNoTracking() on cn.OriginalBillId equals b.BillId into ob
                from bill in ob.DefaultIfEmpty()
                orderby cn.CreditNoteDate descending
                select new CreditNote
                {
                    CreditNoteId    = cn.CreditNoteId,
                    ShopId          = cn.ShopId,
                    CustomerId      = cn.CustomerId,
                    OriginalBillId  = cn.OriginalBillId,
                    CreditNoteDate  = cn.CreditNoteDate,
                    CreditAmount    = cn.CreditAmount,
                    AdjustedAmount  = cn.AdjustedAmount,
                    RemainingCredit = cn.RemainingCredit,
                    Status          = cn.Status,
                    Notes           = cn.Notes,
                    CreatedAt       = cn.CreatedAt,
                    OriginalBill = bill == null
                        ? null
                        : new Bill { BillId = bill.BillId, BillNumber = bill.BillNumber }
                }
            ).Take(creditsTake).ToListAsync();

            return new CustomerLedgerDto
            {
                Customer               = customer,
                Bills                  = bills,
                Payments               = payments,
                CreditNotes            = creditNotes,
                BillsTotalCount        = billsTotalCount,
                PaymentsTotalCount     = paymentsTotalCount,
                CreditNotesTotalCount  = creditNotesTotalCount,
                LedgerTotalBilled      = ledgerTotalBilled,
                LedgerTotalPaidOnBills = ledgerTotalPaidOnBills
            };
        }
    }
}
