using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class ProductCategory
{
    public int CategoryId { get; set; }

    public int ShopId { get; set; }

    public string CategoryName { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual Shop Shop { get; set; }
}
