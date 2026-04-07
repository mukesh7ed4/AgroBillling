using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class StockMovement
{
    public int MovementId { get; set; }

    public int ShopId { get; set; }

    public int ProductId { get; set; }

    public string MovementType { get; set; }

    public string ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public decimal QuantityChange { get; set; }

    public decimal StockBefore { get; set; }

    public decimal StockAfter { get; set; }

    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; }

    public virtual Shop Shop { get; set; }
}
