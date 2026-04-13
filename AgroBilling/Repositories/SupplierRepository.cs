// ================================================
//  AgroBilling.DAL / Repositories / SupplierRepository.cs
// ================================================

using AgroBilling.DAL.Context;
using AgroBilling.DAL.Models;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBilling.DAL.Repositories
{
    public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(AgroBillingDbContext context) : base(context) { }

        // ✅ FIX: Calculate totals in DB — single query with aggregates
        public async Task<IEnumerable<Supplier>> GetByShopIdAsync(int shopId)
        {
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.ShopId == shopId && s.IsActive == true)
                .OrderBy(s => s.CompanyName)
                .ToListAsync();

            // Calculate totals for each supplier in one batch
            var supplierIds = suppliers.Select(s => s.SupplierId).ToList();

            // Total purchased per supplier
            var purchaseTotals = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(p => supplierIds.Contains(p.SupplierId))
                .GroupBy(p => p.SupplierId)
                .Select(g => new
                {
                    SupplierId = g.Key,
                    TotalPurchased = g.Sum(p => p.NetPayable)
                })
                .ToListAsync();

            // Total paid per supplier
            var paymentTotals = await _context.SupplierPayments
                .AsNoTracking()
                .Where(p => supplierIds.Contains(p.SupplierId))
                .GroupBy(p => p.SupplierId)
                .Select(g => new
                {
                    SupplierId = g.Key,
                    TotalPaid = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            // Attach to supplier objects
            foreach (var s in suppliers)
            {
                var purchased = purchaseTotals
                    .FirstOrDefault(x => x.SupplierId == s.SupplierId);
                var paid = paymentTotals
                    .FirstOrDefault(x => x.SupplierId == s.SupplierId);

                s.TotalPurchased = purchased?.TotalPurchased ?? 0;
                s.TotalPaid = paid?.TotalPaid ?? 0;
                s.OutstandingDue = s.OpeningBalance + s.TotalPurchased - s.TotalPaid;
            }

            return suppliers;
        }

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
                    PurchaseId = p.PurchaseId,
                    ShopId = p.ShopId,
                    SupplierId = p.SupplierId,
                    PurchaseDate = p.PurchaseDate,
                    InvoiceNumber = p.InvoiceNumber,
                    TotalAmount = p.TotalAmount,
                    DiscountAmount = p.DiscountAmount,
                    Gstamount = p.Gstamount,
                    NetPayable = p.NetPayable,
                    AmountPaid = p.AmountPaid,
                    PaymentStatus = p.PaymentStatus,
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            var payments = await _context.SupplierPayments
                .AsNoTracking()
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new SupplierPayment
                {
                    PaymentId = p.PaymentId,
                    ShopId = p.ShopId,
                    SupplierId = p.SupplierId,
                    PurchaseId = p.PurchaseId,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    PaymentMode = p.PaymentMode,
                    Reference = p.Reference,
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt,
                    Purchase = p.PurchaseId == null
                        ? null
                        : new PurchaseOrder
                        {
                            PurchaseId = p.PurchaseId.Value,
                            InvoiceNumber = p.Purchase.InvoiceNumber
                        }
                })
                .ToListAsync();

            // ✅ Calculate totals for ledger header stats
            var totalPurchased = purchases.Sum(p => p.NetPayable);
            var totalPaid = payments.Sum(p => p.Amount);

            supplier.TotalPurchased = totalPurchased;
            supplier.TotalPaid = totalPaid;
            supplier.OutstandingDue = supplier.OpeningBalance + totalPurchased - totalPaid;

            return new SupplierLedgerDto
            {
                Supplier = supplier,
                Purchases = purchases,
                Payments = payments
            };
        }
    }
}