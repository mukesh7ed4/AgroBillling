using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class BillPayment
{
    public int PaymentId { get; set; }

    public int BillId { get; set; }

    public int ShopId { get; set; }

    public int CustomerId { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMode { get; set; }

    public string Reference { get; set; }

    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Bill Bill { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual Shop Shop { get; set; }
}
