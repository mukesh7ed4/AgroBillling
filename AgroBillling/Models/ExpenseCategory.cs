using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class ExpenseCategory
{
    public int CategoryId { get; set; }

    public int? ShopId { get; set; }

    public string CategoryName { get; set; }

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
