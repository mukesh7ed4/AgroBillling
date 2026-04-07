using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class CreditNoteItem
{
    public int CreditNoteItemId { get; set; }

    public int CreditNoteId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual CreditNote CreditNote { get; set; }

    public virtual Product Product { get; set; }
}
