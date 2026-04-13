using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroBilling.DAL.Models;

public partial class Product
{
    public int ProductId { get; set; }
    public int ShopId { get; set; }
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    // ✅ FIXED: nullable optional fields
    public string? CompanyName { get; set; }
    public string? Hsncode { get; set; }

    public int UnitId { get; set; }

    [NotMapped] public string? CategoryName => Category?.CategoryName;
    [NotMapped] public string? UnitShortName => Unit?.ShortName;
    [NotMapped] public string? SupplierName => Supplier?.CompanyName;

    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Gstpercent { get; set; }
    public bool UseShopGst { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinStockAlert { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    public virtual ProductCategory Category { get; set; } = null!;
    public virtual ICollection<CreditNoteItem> CreditNoteItems { get; set; } = new List<CreditNoteItem>();
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public virtual Shop Shop { get; set; } = null!;
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual Supplier? Supplier { get; set; }
    public virtual Unit Unit { get; set; } = null!;
}