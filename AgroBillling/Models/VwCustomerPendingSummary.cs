using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class VwCustomerPendingSummary
{
    public int ShopId { get; set; }

    public int CustomerId { get; set; }

    public string FullName { get; set; }

    public string MobileNumber { get; set; }

    public string Village { get; set; }

    public int? TotalBills { get; set; }

    public decimal TotalBilled { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalPending { get; set; }

    public DateOnly? LastBillDate { get; set; }
}
