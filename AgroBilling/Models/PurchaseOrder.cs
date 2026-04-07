using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class PurchaseOrder
{
    public int PurchaseId { get; set; }

    public int ShopId { get; set; }

    public int SupplierId { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public string InvoiceNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Gstamount { get; set; }

    public decimal NetPayable { get; set; }

    public decimal AmountPaid { get; set; }

    public string PaymentStatus { get; set; }

    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    public virtual Shop Shop { get; set; }

    public virtual Supplier Supplier { get; set; }

    public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}
