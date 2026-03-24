using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class Bill
{
    public int BillId { get; set; }

    public int ShopId { get; set; }

    public int CustomerId { get; set; }

    public string BillNumber { get; set; }

    public DateOnly BillDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Gstpercent { get; set; }

    public decimal Gstamount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal? AmountPending { get; set; }

    public string PaymentStatus { get; set; }

    public string Notes { get; set; }

    public bool IsReturn { get; set; }

    public int? OriginalBillId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();

    public virtual ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual Customer Customer { get; set; }

    public virtual ICollection<Bill> InverseOriginalBill { get; set; } = new List<Bill>();

    public virtual Bill OriginalBill { get; set; }

    public virtual Shop Shop { get; set; }
}
