// ================================================
//  AgroBilling.DAL / Repositories / PurchaseRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class PurchaseRepository : GenericRepository<PurchaseOrder>, IPurchaseRepository
    {
        public PurchaseRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<IEnumerable<PurchaseOrder>> GetByShopIdAsync(
            int shopId, int page = 1, int pageSize = 20) =>
            await _context.PurchaseOrders
                .AsNoTracking()
                .Where(p => p.ShopId == shopId)
                .OrderByDescending(p => p.PurchaseDate)
                .ThenByDescending(p => p.PurchaseId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PurchaseOrder
                {
                    PurchaseId      = p.PurchaseId,
                    ShopId          = p.ShopId,
                    SupplierId      = p.SupplierId,
                    PurchaseDate    = p.PurchaseDate,
                    InvoiceNumber   = p.InvoiceNumber,
                    TotalAmount     = p.TotalAmount,
                    DiscountAmount  = p.DiscountAmount,
                    Gstamount       = p.Gstamount,
                    NetPayable      = p.NetPayable,
                    AmountPaid      = p.AmountPaid,
                    PaymentStatus   = p.PaymentStatus,
                    Notes           = p.Notes,
                    CreatedAt       = p.CreatedAt,
                    Supplier = new Supplier
                    {
                        SupplierId  = p.SupplierId,
                        CompanyName = p.Supplier.CompanyName
                    }
                })
                .ToListAsync();

        public async Task<int> GetCountAsync(int shopId) =>
            await _context.PurchaseOrders.CountAsync(p => p.ShopId == shopId);

        public async Task<PurchaseOrder?> GetWithItemsAsync(int purchaseId) =>
            await _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.PurchaseId == purchaseId);

        public async Task AddSupplierPaymentAsync(SupplierPayment payment)
        {
            await _context.SupplierPayments.AddAsync(payment);

            if (payment.PurchaseId.HasValue)
            {
                var purchase = await _context.PurchaseOrders
                    .Include(p => p.SupplierPayments)
                    .FirstOrDefaultAsync(p => p.PurchaseId == payment.PurchaseId.Value);

                if (purchase != null)
                {
                    var totalPaid = purchase.SupplierPayments.Sum(p => p.Amount) + payment.Amount;
                    purchase.AmountPaid    = totalPaid;
                    purchase.PaymentStatus =
                        totalPaid >= purchase.NetPayable ? "PAID" :
                        totalPaid > 0                    ? "PARTIAL" : "PENDING";
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
