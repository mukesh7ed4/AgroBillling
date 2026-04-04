using System;
using System.Collections.Generic;
using AgroBillling.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace AgroBillling.DAL.Context;

public partial class AgroBillingDbContext : DbContext
{
    public AgroBillingDbContext()
    {
    }

    public AgroBillingDbContext(DbContextOptions<AgroBillingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminNotification> AdminNotifications { get; set; }

    public virtual DbSet<AdminUser> AdminUsers { get; set; }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<BillItem> BillItems { get; set; }

    public virtual DbSet<BillPayment> BillPayments { get; set; }

    public virtual DbSet<CreditNote> CreditNotes { get; set; }

    public virtual DbSet<CreditNoteItem> CreditNoteItems { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<ExpenseCategory> ExpenseCategories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<ShopSubscription> ShopSubscriptions { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierPayment> SupplierPayments { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<VwCustomerPendingSummary> VwCustomerPendingSummaries { get; set; }

    public virtual DbSet<VwSupplierOutstanding> VwSupplierOutstandings { get; set; }

    public virtual DbSet<PaymentRequest> PaymentRequests { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.NotificationType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Shop).WithMany(p => p.AdminNotifications)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.AdminId);

            entity.HasIndex(e => e.Email, "UQ__AdminUse__A9D10534971C40F7").IsUnique();

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.BillId);

            entity.HasIndex(e => new { e.ShopId, e.BillDate, e.PaymentStatus }, "IX_Bills_ShopID_Date_Status")
                .IsDescending(false, true, false)
                .IncludeProperties(e => new
                {
                    e.CustomerId,
                    e.TotalAmount,
                    e.AmountPaid,
                    e.AmountPending,
                    e.BillNumber
                });

            entity.HasIndex(e => new { e.ShopId, e.CustomerId }, "IX_Bills_ShopID_CustomerID");

            entity.HasIndex(e => new { e.ShopId, e.BillNumber }, "UQ__Bills__0B8EE82491D106D5").IsUnique();

            entity.Property(e => e.BillId).HasColumnName("BillID");
            entity.Property(e => e.AmountPaid).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.AmountPending)
    .HasComputedColumnSql("\"TotalAmount\" - \"AmountPaid\"", stored: true)
    .HasColumnType("numeric(13, 2)");
            entity.Property(e => e.BillDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.BillNumber)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.Gstamount)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.OriginalBillId).HasColumnName("OriginalBillID");
            entity.Property(e => e.PaymentStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SubTotal).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bills)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OriginalBill).WithMany(p => p.InverseOriginalBill)
                .HasForeignKey(d => d.OriginalBillId);

            entity.HasOne(d => d.Shop).WithMany(p => p.Bills)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<BillItem>(entity =>
        {
            entity.HasKey(e => e.BillItemId);

            entity.HasIndex(e => e.BillId, "IX_BillItems_BillID");

            entity.Property(e => e.BillItemId).HasColumnName("BillItemID");
            entity.Property(e => e.BillId).HasColumnName("BillID");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.Gstamount)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnType("numeric(12, 3)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(10, 2)");

            entity.HasOne(d => d.Bill).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Product).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<BillPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.HasIndex(e => e.BillId, "IX_BillPayments_BillID");

            entity.HasIndex(e => e.CustomerId, "IX_BillPayments_CustomerID");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.BillId).HasColumnName("BillID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.PaymentMode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Cash");
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Bill).WithMany(p => p.BillPayments)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Customer).WithMany(p => p.BillPayments)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.BillPayments)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CreditNote>(entity =>
        {
            entity.HasKey(e => e.CreditNoteId);

            entity.Property(e => e.CreditNoteId).HasColumnName("CreditNoteID");
            entity.Property(e => e.AdjustedAmount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.CreditAmount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.CreditNoteDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.OriginalBillId).HasColumnName("OriginalBillID");
            entity.Property(e => e.RemainingCredit)
    .HasComputedColumnSql("\"CreditAmount\" - \"AdjustedAmount\"", stored: true)
    .HasColumnType("numeric(13, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("OPEN");

            entity.HasOne(d => d.Customer).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OriginalBill).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.OriginalBillId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CreditNoteItem>(entity =>
        {
            entity.HasKey(e => e.CreditNoteItemId);

            entity.Property(e => e.CreditNoteItemId).HasColumnName("CreditNoteItemID");
            entity.Property(e => e.CreditNoteId).HasColumnName("CreditNoteID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnType("numeric(12, 3)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(10, 2)");

            entity.HasOne(d => d.CreditNote).WithMany(p => p.CreditNoteItems)
                .HasForeignKey(d => d.CreditNoteId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Product).WithMany(p => p.CreditNoteItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);

            entity.HasIndex(e => new { e.ShopId, e.MobileNumber }, "IX_Customers_ShopID_Mobile");

            entity.HasIndex(e => new { e.ShopId, e.FullName }, "IX_Customers_ShopID_Name")
                .IncludeProperties(e => new { e.MobileNumber, e.Village, e.District });

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.AadhaarLast4).HasMaxLength(4);
            entity.Property(e => e.AlternateMobile).HasMaxLength(15);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.FatherName).HasMaxLength(100);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LandAcres).HasColumnType("numeric(8, 2)");
            entity.Property(e => e.MobileNumber)
                .IsRequired()
                .HasMaxLength(15);
            entity.Property(e => e.OpeningBalance).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.State)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Haryana");
            entity.Property(e => e.Tehsil).HasMaxLength(100);
            entity.Property(e => e.Village).HasMaxLength(100);

            entity.HasOne(d => d.Shop).WithMany(p => p.Customers)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId);

            entity.HasIndex(e => new { e.ShopId, e.ExpenseDate }, "IX_Expenses_ShopID_Date")
                .IsDescending(false, true)
                .IncludeProperties(e => new { e.Amount, e.CategoryId });

            entity.Property(e => e.ExpenseId).HasColumnName("ExpenseID");
            entity.Property(e => e.Amount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.ExpenseDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.PaymentMode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Cash");
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Category).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);

            entity.HasIndex(e => new { e.ShopId, e.CategoryId }, "IX_Products_ShopID_Category");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CompanyName).HasMaxLength(150);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.CurrentStock).HasColumnType("numeric(12, 3)");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.Hsncode)
                .HasMaxLength(20)
                .HasColumnName("HSNCode");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MinStockAlert)
                .HasDefaultValue(5.000m)
                .HasColumnType("numeric(12, 3)");
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.PurchasePrice).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.SellingPrice).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.UseShopGst)
                .HasDefaultValue(true)
                .HasColumnName("UseShopGST");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.Products)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId);

            entity.HasOne(d => d.Unit).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Shop).WithMany(p => p.ProductCategories)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseId);

            entity.HasIndex(e => new { e.ShopId, e.SupplierId }, "IX_PurchaseOrders_SupplierID");

            entity.HasIndex(e => new { e.ShopId, e.PurchaseDate }, "IX_PurchaseOrders_ShopID_Date")
                .IsDescending(false, true)
                .IncludeProperties(e => new
                {
                    e.SupplierId,
                    e.NetPayable,
                    e.AmountPaid,
                    e.PaymentStatus
                });

            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.AmountPaid).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.Gstamount)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(50);
            entity.Property(e => e.NetPayable).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.PaymentStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.PurchaseDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12, 2)");

            entity.HasOne(d => d.Shop).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.PurchaseItemId);

            entity.Property(e => e.PurchaseItemId).HasColumnName("PurchaseItemID");
            entity.Property(e => e.Gstamount)
                .HasColumnType("numeric(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.Quantity).HasColumnType("numeric(12, 3)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(10, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.ShopId);

            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(300);
            entity.Property(e => e.AlternateMobile).HasMaxLength(15);
            entity.Property(e => e.BillStartNumber).HasDefaultValue(1);
            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.CreatedByAdminId).HasColumnName("CreatedByAdminID");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(20)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.Gstpercent)
                .HasDefaultValue(18.00m)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LogoPath).HasMaxLength(300);
            // ✅ OTP fields
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.EmailOtp).HasMaxLength(6);
            entity.Property(e => e.OtpExpiresAt);
            entity.Property(e => e.MobileNumber)
                .IsRequired()
                .HasMaxLength(15);
            entity.Property(e => e.OwnerName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.PinCode)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.ShopName)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.State)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Haryana");

            entity.HasOne(d => d.CreatedByAdmin).WithMany(p => p.Shops)
                .HasForeignKey(d => d.CreatedByAdminId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ShopSubscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);

            entity.HasIndex(e => new { e.ShopId, e.EndDate }, "IX_ShopSubscriptions_ShopID");

            entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionID");
            entity.Property(e => e.AmountPaid).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.ExtendedByAdminId).HasColumnName("ExtendedByAdminID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.PaymentMode).HasMaxLength(50);
            entity.Property(e => e.PaymentReference).HasMaxLength(100);
            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.ExtendedByAdmin).WithMany(p => p.ShopSubscriptions)
                .HasForeignKey(d => d.ExtendedByAdminId);

            entity.HasOne(d => d.Plan).WithMany(p => p.ShopSubscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopSubscriptions)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId);

            entity.HasIndex(e => new { e.ShopId, e.ProductId }, "IX_StockMovements_ProductID");

            entity.Property(e => e.MovementId).HasColumnName("MovementID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.MovementType)
                .IsRequired()
                .HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.QuantityChange).HasColumnType("numeric(12, 3)");
            entity.Property(e => e.ReferenceId).HasColumnName("ReferenceID");
            entity.Property(e => e.ReferenceType).HasMaxLength(30);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.StockAfter).HasColumnType("numeric(12, 3)");
            entity.Property(e => e.StockBefore).HasColumnType("numeric(12, 3)");

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId);

            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PlanName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("numeric(10, 2)");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId);

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.CompanyName)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.ContactPersonName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(20)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MobileNumber).HasMaxLength(15);
            entity.Property(e => e.OpeningBalance).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Shop).WithMany(p => p.Suppliers)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SupplierPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.PaymentMode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Cash");
            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

            entity.HasOne(d => d.Purchase).WithMany(p => p.SupplierPayments)
                .HasForeignKey(d => d.PurchaseId);

            entity.HasOne(d => d.Shop).WithMany(p => p.SupplierPayments)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplierPayments)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.UnitId);

            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.ShortName)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.UnitName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<VwCustomerPendingSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CustomerPendingSummary");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.MobileNumber)
                .IsRequired()
                .HasMaxLength(15);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.TotalBilled).HasColumnType("numeric(38, 2)");
            entity.Property(e => e.TotalPaid).HasColumnType("numeric(38, 2)");
            entity.Property(e => e.TotalPending).HasColumnType("numeric(38, 2)");
            entity.Property(e => e.Village).HasMaxLength(100);
        });

        modelBuilder.Entity<VwSupplierOutstanding>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SupplierOutstanding");

            entity.Property(e => e.CompanyName)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.OpeningBalance).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.OutstandingDue).HasColumnType("numeric(38, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalPaid).HasColumnType("numeric(38, 2)");
            entity.Property(e => e.TotalPurchased).HasColumnType("numeric(38, 2)");
        });
        modelBuilder.Entity<PaymentRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.ApprovedByAdminId).HasColumnName("ApprovedByAdminID");
            entity.Property(e => e.Amount).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.TransactionId)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PayerName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PayerMobile).HasMaxLength(15);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.AdminNotes).HasMaxLength(500);
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(d => d.Shop)
                .WithMany()
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Plan)
                .WithMany()
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }


    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}