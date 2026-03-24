using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class PurchaseOrderItem
{
    public int PurchaseItemId { get; set; }

    public int PurchaseId { get; set; }

    public int ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Gstpercent { get; set; }

    public decimal Gstamount { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual Product Product { get; set; }

    public virtual PurchaseOrder Purchase { get; set; }
}
