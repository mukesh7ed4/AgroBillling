using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroBilling.DAL.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }
    public int ShopId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    // ✅ FIXED: nullable strings — these are optional in DB
    public string? ContactPersonName { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Gstnumber { get; set; }

    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    [NotMapped] public decimal TotalPurchased { get; set; }
    [NotMapped] public decimal TotalPaid { get; set; }
    [NotMapped] public decimal OutstandingDue { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual Shop Shop { get; set; } = null!;
    public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}