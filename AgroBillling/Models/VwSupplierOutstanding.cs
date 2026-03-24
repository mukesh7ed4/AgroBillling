using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class VwSupplierOutstanding
{
    public int ShopId { get; set; }

    public int SupplierId { get; set; }

    public string CompanyName { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal TotalPurchased { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal? OutstandingDue { get; set; }
}
