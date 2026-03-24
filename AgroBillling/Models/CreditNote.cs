using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class CreditNote
{
    public int CreditNoteId { get; set; }

    public int ShopId { get; set; }

    public int CustomerId { get; set; }

    public int OriginalBillId { get; set; }

    public DateOnly CreditNoteDate { get; set; }

    public decimal CreditAmount { get; set; }

    public decimal AdjustedAmount { get; set; }

    public decimal? RemainingCredit { get; set; }

    public string Status { get; set; }

    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CreditNoteItem> CreditNoteItems { get; set; } = new List<CreditNoteItem>();

    public virtual Customer Customer { get; set; }

    public virtual Bill OriginalBill { get; set; }

    public virtual Shop Shop { get; set; }
}
