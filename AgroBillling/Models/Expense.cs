using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class Expense
{
    public int ExpenseId { get; set; }

    public int ShopId { get; set; }

    public int CategoryId { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; }

    public string PaymentMode { get; set; }

    public string Reference { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ExpenseCategory Category { get; set; }

    public virtual Shop Shop { get; set; }
}
