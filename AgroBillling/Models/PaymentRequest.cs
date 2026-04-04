using System;

namespace AgroBillling.DAL.Models;

public class PaymentRequest
{
    public int RequestId { get; set; }
    public int ShopId { get; set; }
    public int PlanId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerMobile { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? AdminNotes { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByAdminId { get; set; }

    public virtual Shop Shop { get; set; } = null!;
    public virtual SubscriptionPlan Plan { get; set; } = null!;
}