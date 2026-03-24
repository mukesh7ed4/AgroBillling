// ================================================
//  AgroBilling.DAL / Repositories / SupplierRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<IEnumerable<Supplier>> GetByShopIdAsync(int shopId) =>
            await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.ShopId == shopId && s.IsActive == true)
                .OrderBy(s => s.CompanyName)
                .ToListAsync();

        public async Task<SupplierLedgerDto> GetLedgerAsync(int supplierId)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SupplierId == supplierId);
            if (supplier == null) return new SupplierLedgerDto();

            var purchases = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PurchaseDate)
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
                    CreatedAt       = p.CreatedAt
                })
                .ToListAsync();

            var payments = await _context.SupplierPayments
                .AsNoTracking()
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new SupplierPayment
                {
                    PaymentId   = p.PaymentId,
                    ShopId      = p.ShopId,
                    SupplierId  = p.SupplierId,
                    PurchaseId  = p.PurchaseId,
                    PaymentDate = p.PaymentDate,
                    Amount      = p.Amount,
                    PaymentMode = p.PaymentMode,
                    Reference   = p.Reference,
                    Notes       = p.Notes,
                    CreatedAt   = p.CreatedAt,
                    Purchase = p.PurchaseId == null
                        ? null
                        : new PurchaseOrder
                        {
                            PurchaseId    = p.PurchaseId.Value,
                            InvoiceNumber = p.Purchase.InvoiceNumber
                        }
                })
                .ToListAsync();

            return new SupplierLedgerDto
            {
                Supplier  = supplier,
                Purchases = purchases,
                Payments  = payments
            };
        }
    }
}
