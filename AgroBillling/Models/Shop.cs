using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class Shop
{
    public int ShopId { get; set; }

    public string OwnerName { get; set; }

    public string ShopName { get; set; }

    public string MobileNumber { get; set; }

    public string AlternateMobile { get; set; }

    public string Email { get; set; }

    public string Address { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string PinCode { get; set; }

    public string Gstnumber { get; set; }

    public decimal Gstpercent { get; set; }


    public int BillStartNumber { get; set; }

    public int CurrentBillSequence { get; set; }

    public string PasswordHash { get; set; }

    public string LogoPath { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedByAdminId { get; set; }

    public virtual ICollection<AdminNotification> AdminNotifications { get; set; } = new List<AdminNotification>();

    public virtual ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual AdminUser CreatedByAdmin { get; set; }

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<ShopSubscription> ShopSubscriptions { get; set; } = new List<ShopSubscription>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();

    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
