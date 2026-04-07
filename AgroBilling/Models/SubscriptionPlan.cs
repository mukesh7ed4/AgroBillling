using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class SubscriptionPlan
{
    public int PlanId { get; set; }

    public string PlanName { get; set; }

    public int DurationDays { get; set; }

    public decimal Price { get; set; }

    public bool IsTrial { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ShopSubscription> ShopSubscriptions { get; set; } = new List<ShopSubscription>();
}
