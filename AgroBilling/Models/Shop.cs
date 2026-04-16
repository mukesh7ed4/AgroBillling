using System;
using System.Collections.Generic;

namespace AgroBilling.DAL.Models;

public partial class Shop
{
    public int ShopId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? AlternateMobile { get; set; }
    public string? Email { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = "Haryana";
    public string PinCode { get; set; } = string.Empty;
    public string? Gstnumber { get; set; }
    public decimal Gstpercent { get; set; } = 18;
    public int BillStartNumber { get; set; } = 1;
    public int CurrentBillSequence { get; set; } = 0;
    public string PasswordHash { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailOtp { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int CreatedByAdminId { get; set; }

    // Navigation properties
    public virtual AdminUser CreatedByAdmin { get; set; } = null!;
    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public virtual ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();
    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<ShopSubscription> ShopSubscriptions { get; set; } = new List<ShopSubscription>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
    public virtual ICollection<AdminNotification> AdminNotifications { get; set; } = new List<AdminNotification>();
}