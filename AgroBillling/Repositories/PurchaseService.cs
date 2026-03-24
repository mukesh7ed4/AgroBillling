// ================================================
//  AgroBilling.DAL / Repositories / PurchaseService.cs
// ================================================

using System.Linq;
using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public interface IPurchaseService
    {
        Task<PurchaseOrder> CreatePurchaseAsync(int shopId, CreatePurchaseDto dto);
    }

    public class PurchaseService : IPurchaseService
    {
        private readonly AgroBillingDbContext _context;

        public PurchaseService(AgroBillingDbContext context)
        {
            _context = context;
        }

        public async Task<PurchaseOrder> CreatePurchaseAsync(int shopId, CreatePurchaseDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var totalAmount = dto.Items.Sum(i => i.TotalAmount);
                var gstTotal    = dto.Items.Sum(i => i.GstAmount);
                var status      = dto.AmountPaid >= totalAmount ? "PAID" :
                                  dto.AmountPaid > 0            ? "PARTIAL" : "PENDING";

                var purchase = new PurchaseOrder
                {
                    ShopId        = shopId,
                    SupplierId    = dto.SupplierId,
                    PurchaseDate  = dto.PurchaseDate,
                    InvoiceNumber = dto.InvoiceNumber,
                    TotalAmount   = totalAmount,
                    Gstamount     = gstTotal,
                    NetPayable    = totalAmount,
                    AmountPaid    = dto.AmountPaid,
                    PaymentStatus = status,
                    Notes         = dto.Notes,
                    CreatedAt     = DateTime.Now
                };

                await _context.PurchaseOrders.AddAsync(purchase);
                await _context.SaveChangesAsync();

                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                // Add items + update stock
                foreach (var item in dto.Items)
                {
                    await _context.PurchaseOrderItems.AddAsync(new PurchaseOrderItem
                    {
                        PurchaseId  = purchase.PurchaseId,
                        ProductId   = item.ProductId,
                        Quantity    = item.Quantity,
                        UnitPrice   = item.UnitPrice,
                        Gstpercent  = item.GstPercent,
                        Gstamount   = item.GstAmount,
                        TotalAmount = item.TotalAmount
                    });

                    products.TryGetValue(item.ProductId, out var product);
                    if (product != null)
                    {
                        var stockBefore = product.CurrentStock;
                        product.CurrentStock += item.Quantity;

                        await _context.StockMovements.AddAsync(new StockMovement
                        {
                            ShopId         = shopId,
                            ProductId      = item.ProductId,
                            MovementType   = "PURCHASE",
                            ReferenceType  = "PURCHASE_ORDER",
                            ReferenceId    = purchase.PurchaseId,
                            QuantityChange = item.Quantity,
                            StockBefore    = stockBefore,
                            StockAfter     = product.CurrentStock,
                            CreatedAt      = DateTime.Now
                        });
                    }
                    else
                    {
                        await _context.StockMovements.AddAsync(new StockMovement
                        {
                            ShopId         = shopId,
                            ProductId      = item.ProductId,
                            MovementType   = "PURCHASE",
                            ReferenceType  = "PURCHASE_ORDER",
                            ReferenceId    = purchase.PurchaseId,
                            QuantityChange = item.Quantity,
                            StockBefore    = -item.Quantity,
                            StockAfter     = 0,
                            CreatedAt      = DateTime.Now
                        });
                    }
                }

                // Record supplier payment if any
                if (dto.AmountPaid > 0)
                {
                    await _context.SupplierPayments.AddAsync(new SupplierPayment
                    {
                        ShopId      = shopId,
                        SupplierId  = dto.SupplierId,
                        PurchaseId  = purchase.PurchaseId,
                        PaymentDate = dto.PurchaseDate,
                        Amount      = dto.AmountPaid,
                        PaymentMode = dto.PaymentMode,
                        CreatedAt   = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return purchase;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
