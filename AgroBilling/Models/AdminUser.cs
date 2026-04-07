using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class AdminUser
{
    public int AdminId { get; set; }

    public string FullName { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ShopSubscription> ShopSubscriptions { get; set; } = new List<ShopSubscription>();

    public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();
}
