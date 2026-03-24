using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class ShopSubscription
{
    public int SubscriptionId { get; set; }

    public int ShopId { get; set; }

    public int PlanId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal AmountPaid { get; set; }

    public string PaymentMode { get; set; }

    public string PaymentReference { get; set; }

    public bool IsActive { get; set; }

    public int? ExtendedByAdminId { get; set; }

    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AdminUser ExtendedByAdmin { get; set; }

    public virtual SubscriptionPlan Plan { get; set; }

    public virtual Shop Shop { get; set; }
}
