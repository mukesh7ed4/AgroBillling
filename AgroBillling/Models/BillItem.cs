using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class BillItem
{
    public int BillItemId { get; set; }

    public int BillId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Gstpercent { get; set; }

    public decimal Gstamount { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual Bill Bill { get; set; }

    public virtual Product Product { get; set; }
}
