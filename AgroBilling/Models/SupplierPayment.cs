using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class SupplierPayment
{
    public int PaymentId { get; set; }

    public int ShopId { get; set; }

    public int SupplierId { get; set; }

    public int? PurchaseId { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMode { get; set; }

    public string Reference { get; set; }

    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual PurchaseOrder Purchase { get; set; }

    public virtual Shop Shop { get; set; }

    public virtual Supplier Supplier { get; set; }
}
