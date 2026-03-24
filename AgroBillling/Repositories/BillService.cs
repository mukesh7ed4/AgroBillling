// ================================================
//  AgroBilling.DAL / Repositories / BillService.cs
//  Complex business logic — create bill, process return
// ================================================

using System.Collections.Generic;
using System.Linq;
using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public interface IBillService
    {
        Task<Bill> CreateBillAsync(int shopId, CreateBillDto dto);
        Task<BillPayment> AddPaymentAsync(AddPaymentDto dto);
        Task<CreditNote> ProcessReturnAsync(int shopId, CreateReturnDto dto);
    }

    public class BillService : IBillService
    {
        private readonly AgroBillingDbContext _context;
        private readonly IShopRepository _shopRepo;

        public BillService(AgroBillingDbContext context, IShopRepository shopRepo)
        {
            _context  = context;
            _shopRepo = shopRepo;
        }

        public async Task<Bill> CreateBillAsync(int shopId, CreateBillDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                // One SQL round-trip; no SaveChanges on Shops (was a major slowdown).
                var billNo = await _shopRepo.GetNextBillNumberAsync(shopId);

                var subTotal  = dto.Items.Sum(i => i.TotalAmount);
                var gstAmount = Math.Round(subTotal * dto.GstPercent / 100, 2);
                var total     = subTotal - dto.DiscountAmount + gstAmount;
                var status    = dto.AmountPaid >= total ? "PAID" :
                                dto.AmountPaid > 0      ? "PARTIAL" : "PENDING";

                var bill = new Bill
                {
                    ShopId         = shopId,
                    CustomerId     = dto.CustomerId,
                    BillNumber     = billNo.ToString(),
                    BillDate       = dto.BillDate,
                    SubTotal       = subTotal,
                    DiscountAmount = dto.DiscountAmount,
                    Gstpercent     = dto.GstPercent,
                    Gstamount      = gstAmount,
                    TotalAmount    = total,
                    AmountPaid     = dto.AmountPaid,
                    PaymentStatus  = status,
                    Notes          = dto.Notes,
                    IsReturn       = false,
                    CreatedAt      = DateTime.Now
                };

                await _context.Bills.AddAsync(bill);
                await _context.SaveChangesAsync();

                var billItems = new List<BillItem>(dto.Items.Count);
                var movements = new List<StockMovement>(dto.Items.Count);
                var now = DateTime.Now;

                foreach (var item in dto.Items)
                {
                    billItems.Add(new BillItem
                    {
                        BillId         = bill.BillId,
                        ProductId      = item.ProductId,
                        ProductName    = item.ProductName,
                        Quantity       = item.Quantity,
                        UnitPrice      = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        Gstpercent     = item.GstPercent,
                        Gstamount      = item.GstAmount,
                        TotalAmount    = item.TotalAmount
                    });

                    products.TryGetValue(item.ProductId, out var product);
                    if (product != null)
                    {
                        var stockBefore = product.CurrentStock;
                        product.CurrentStock -= item.Quantity;
                        movements.Add(new StockMovement
                        {
                            ShopId         = shopId,
                            ProductId      = item.ProductId,
                            MovementType   = "SALE",
                            ReferenceType  = "BILL",
                            ReferenceId    = bill.BillId,
                            QuantityChange = -item.Quantity,
                            StockBefore    = stockBefore,
                            StockAfter     = product.CurrentStock,
                            CreatedAt      = now
                        });
                    }
                    else
                    {
                        movements.Add(new StockMovement
                        {
                            ShopId         = shopId,
                            ProductId      = item.ProductId,
                            MovementType   = "SALE",
                            ReferenceType  = "BILL",
                            ReferenceId    = bill.BillId,
                            QuantityChange = -item.Quantity,
                            StockBefore    = item.Quantity,
                            StockAfter     = 0,
                            CreatedAt      = now
                        });
                    }
                }

                _context.BillItems.AddRange(billItems);
                _context.StockMovements.AddRange(movements);

                if (dto.AmountPaid > 0)
                {
                    await _context.BillPayments.AddAsync(new BillPayment
                    {
                        BillId      = bill.BillId,
                        ShopId      = shopId,
                        CustomerId  = dto.CustomerId,
                        PaymentDate = dto.BillDate,
                        Amount      = dto.AmountPaid,
                        PaymentMode = "Cash",
                        CreatedAt   = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return bill;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BillPayment> AddPaymentAsync(AddPaymentDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.BillId == dto.BillId)
                    ?? throw new Exception("Bill not found");

                var paidBefore = await _context.BillPayments
                    .Where(p => p.BillId == dto.BillId)
                    .SumAsync(p => p.Amount);

                var payment = new BillPayment
                {
                    BillId      = dto.BillId,
                    ShopId      = bill.ShopId,
                    CustomerId  = bill.CustomerId,
                    PaymentDate = dto.PaymentDate,
                    Amount      = dto.Amount,
                    PaymentMode = dto.PaymentMode,
                    Reference   = dto.Reference,
                    Notes       = dto.Notes,
                    CreatedAt   = DateTime.Now
                };

                await _context.BillPayments.AddAsync(payment);

                var totalPaid = paidBefore + dto.Amount;
                bill.AmountPaid = totalPaid;
                bill.PaymentStatus =
                    totalPaid >= bill.TotalAmount ? "PAID" :
                    totalPaid > 0                 ? "PARTIAL" : "PENDING";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return payment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CreditNote> ProcessReturnAsync(int shopId, CreateReturnDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var creditAmount = dto.Items.Sum(i => i.TotalAmount);

                var creditNote = new CreditNote
                {
                    ShopId         = shopId,
                    CustomerId     = dto.CustomerId,
                    OriginalBillId = dto.OriginalBillId,
                    CreditNoteDate = dto.ReturnDate,
                    CreditAmount   = creditAmount,
                    AdjustedAmount = 0,
                    Status         = "OPEN",
                    Notes          = dto.Notes,
                    CreatedAt      = DateTime.Now
                };

                await _context.CreditNotes.AddAsync(creditNote);
                await _context.SaveChangesAsync();

                var returnProductIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
                var returnProducts = await _context.Products
                    .Where(p => returnProductIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                var cnItems = new List<CreditNoteItem>(dto.Items.Count);
                var movements = new List<StockMovement>(dto.Items.Count);
                var now = DateTime.Now;

                foreach (var item in dto.Items)
                {
                    cnItems.Add(new CreditNoteItem
                    {
                        CreditNoteId = creditNote.CreditNoteId,
                        ProductId    = item.ProductId,
                        ProductName  = item.ProductName,
                        Quantity     = item.Quantity,
                        UnitPrice    = item.UnitPrice,
                        TotalAmount  = item.TotalAmount
                    });

                    returnProducts.TryGetValue(item.ProductId, out var product);
                    if (product != null)
                    {
                        var stockBefore = product.CurrentStock;
                        product.CurrentStock += item.Quantity;
                        movements.Add(new StockMovement
                        {
                            ShopId         = shopId,
                            ProductId      = item.ProductId,
                            MovementType   = "RETURN_IN",
                            ReferenceType  = "CREDIT_NOTE",
                            ReferenceId    = creditNote.CreditNoteId,
                            QuantityChange = item.Quantity,
                            StockBefore    = stockBefore,
                            StockAfter     = product.CurrentStock,
                            CreatedAt      = now
                        });
                    }
                    else
                    {
                        movements.Add(new StockMovement
                        {
                            ShopId         = shopId,
                            ProductId      = item.ProductId,
                            MovementType   = "RETURN_IN",
                            ReferenceType  = "CREDIT_NOTE",
                            ReferenceId    = creditNote.CreditNoteId,
                            QuantityChange = item.Quantity,
                            StockBefore    = -item.Quantity,
                            StockAfter     = 0,
                            CreatedAt      = now
                        });
                    }
                }

                _context.CreditNoteItems.AddRange(cnItems);
                _context.StockMovements.AddRange(movements);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return creditNote;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
