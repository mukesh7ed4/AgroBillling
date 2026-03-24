using System;
using System.Collections.Generic;
using AgroBillling.DAL.Models;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Design-time / tools only. Runtime uses connection from Program.cs — do not override when already configured.
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=AgroBillingDB;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__AdminNot__20CF2E327D375037");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.NotificationType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Shop).WithMany(p => p.AdminNotifications)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AdminNoti__ShopI__66603565");
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__AdminUse__719FE4E814A1C4A7");

            entity.HasIndex(e => e.Email, "UQ__AdminUse__A9D10534971C40F7").IsUnique();

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
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
            entity.HasKey(e => e.BillId).HasName("PK__Bills__11F2FC4AA014A4C0");

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
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.AmountPending)
                .HasComputedColumnSql("([TotalAmount]-[AmountPaid])", true)
                .HasColumnType("decimal(13, 2)");
            entity.Property(e => e.BillDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.BillNumber)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Gstamount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.OriginalBillId).HasColumnName("OriginalBillID");
            entity.Property(e => e.PaymentStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bills)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Bills__CustomerI__282DF8C2");

            entity.HasOne(d => d.OriginalBill).WithMany(p => p.InverseOriginalBill)
                .HasForeignKey(d => d.OriginalBillId)
                .HasConstraintName("FK__Bills__OriginalB__29221CFB");

            entity.HasOne(d => d.Shop).WithMany(p => p.Bills)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Bills__ShopID__2739D489");
        });

        modelBuilder.Entity<BillItem>(entity =>
        {
            entity.HasKey(e => e.BillItemId).HasName("PK__BillItem__47605AD51E5D4B61");

            entity.HasIndex(e => e.BillId, "IX_BillItems_BillID");

            entity.Property(e => e.BillItemId).HasColumnName("BillItemID");
            entity.Property(e => e.BillId).HasColumnName("BillID");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Gstamount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Bill).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillItems__BillI__2EDAF651");

            entity.HasOne(d => d.Product).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillItems__Produ__2FCF1A8A");
        });

        modelBuilder.Entity<BillPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__BillPaym__9B556A58FCF3AC08");

            entity.HasIndex(e => e.BillId, "IX_BillPayments_BillID");

            entity.HasIndex(e => e.CustomerId, "IX_BillPayments_CustomerID");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.BillId).HasColumnName("BillID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.PaymentMode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Cash");
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Bill).WithMany(p => p.BillPayments)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillPayme__BillI__3587F3E0");

            entity.HasOne(d => d.Customer).WithMany(p => p.BillPayments)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillPayme__Custo__37703C52");

            entity.HasOne(d => d.Shop).WithMany(p => p.BillPayments)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillPayme__ShopI__367C1819");
        });

        modelBuilder.Entity<CreditNote>(entity =>
        {
            entity.HasKey(e => e.CreditNoteId).HasName("PK__CreditNo__AF360DA6E4D60418");

            entity.Property(e => e.CreditNoteId).HasColumnName("CreditNoteID");
            entity.Property(e => e.AdjustedAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreditAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreditNoteDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.OriginalBillId).HasColumnName("OriginalBillID");
            entity.Property(e => e.RemainingCredit)
                .HasComputedColumnSql("([CreditAmount]-[AdjustedAmount])", true)
                .HasColumnType("decimal(13, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("OPEN");

            entity.HasOne(d => d.Customer).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CreditNot__Custo__3F115E1A");

            entity.HasOne(d => d.OriginalBill).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.OriginalBillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CreditNot__Origi__40058253");

            entity.HasOne(d => d.Shop).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CreditNot__ShopI__3E1D39E1");
        });

        modelBuilder.Entity<CreditNoteItem>(entity =>
        {
            entity.HasKey(e => e.CreditNoteItemId).HasName("PK__CreditNo__58E8CFB99B1509E5");

            entity.Property(e => e.CreditNoteItemId).HasColumnName("CreditNoteItemID");
            entity.Property(e => e.CreditNoteId).HasColumnName("CreditNoteID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.CreditNote).WithMany(p => p.CreditNoteItems)
                .HasForeignKey(d => d.CreditNoteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CreditNot__Credi__42E1EEFE");

            entity.HasOne(d => d.Product).WithMany(p => p.CreditNoteItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CreditNot__Produ__43D61337");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B888C6E9DC");

            entity.HasIndex(e => new { e.ShopId, e.MobileNumber }, "IX_Customers_ShopID_Mobile");

            entity.HasIndex(e => new { e.ShopId, e.FullName }, "IX_Customers_ShopID_Name")
                .IncludeProperties(e => new { e.MobileNumber, e.Village, e.District });

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.AadhaarLast4).HasMaxLength(4);
            entity.Property(e => e.AlternateMobile).HasMaxLength(15);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.FatherName).HasMaxLength(100);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LandAcres).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.MobileNumber)
                .IsRequired()
                .HasMaxLength(15);
            entity.Property(e => e.OpeningBalance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.State)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Haryana");
            entity.Property(e => e.Tehsil).HasMaxLength(100);
            entity.Property(e => e.Village).HasMaxLength(100);

            entity.HasOne(d => d.Shop).WithMany(p => p.Customers)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Customers__ShopI__1BC821DD");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId).HasName("PK__Expenses__1445CFF360FFA9D8");

            entity.HasIndex(e => new { e.ShopId, e.ExpenseDate }, "IX_Expenses_ShopID_Date")
                .IsDescending(false, true)
                .IncludeProperties(e => new { e.Amount, e.CategoryId });

            entity.Property(e => e.ExpenseId).HasColumnName("ExpenseID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.ExpenseDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.PaymentMode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Cash");
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Category).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Expenses__Catego__4E53A1AA");

            entity.HasOne(d => d.Shop).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Expenses__ShopID__4D5F7D71");
        });

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ExpenseC__19093A2BF7E5060B");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED319D3FF3");

            entity.HasIndex(e => new { e.ShopId, e.CategoryId }, "IX_Products_ShopID_Category");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CompanyName).HasMaxLength(150);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CurrentStock).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.Hsncode)
                .HasMaxLength(20)
                .HasColumnName("HSNCode");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MinStockAlert)
                .HasDefaultValue(5.000m)
                .HasColumnType("decimal(12, 3)");
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.UseShopGst)
                .HasDefaultValue(true)
                .HasColumnName("UseShopGST");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__Catego__7C4F7684");

            entity.HasOne(d => d.Shop).WithMany(p => p.Products)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__ShopID__7B5B524B");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__Products__Suppli__7D439ABD");

            entity.HasOne(d => d.Unit).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__UnitID__7E37BEF6");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ProductC__19093A2BFC5A26A2");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Shop).WithMany(p => p.ProductCategories)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductCa__ShopI__6A30C649");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseId).HasName("PK__Purchase__6B0A6BDE6BB9DA68");

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
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Gstamount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(50);
            entity.Property(e => e.NetPayable).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.PaymentStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.PurchaseDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Shop).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseO__ShopI__06CD04F7");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseO__Suppl__07C12930");
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.PurchaseItemId).HasName("PK__Purchase__B48BB6A788F765EA");

            entity.Property(e => e.PurchaseItemId).HasColumnName("PurchaseItemID");
            entity.Property(e => e.Gstamount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("GSTAmount");
            entity.Property(e => e.Gstpercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseO__Produ__0D7A0286");

            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseO__Purch__0C85DE4D");
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.ShopId).HasName("PK__Shops__67C5562919C41156");

            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(300);
            entity.Property(e => e.AlternateMobile).HasMaxLength(15);
            entity.Property(e => e.BillStartNumber).HasDefaultValue(1);
            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreatedByAdminId).HasColumnName("CreatedByAdminID");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(20)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.Gstpercent)
                .HasDefaultValue(18.00m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("GSTPercent");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LogoPath).HasMaxLength(300);
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Shops__CreatedBy__59FA5E80");
        });

        modelBuilder.Entity<ShopSubscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__ShopSubs__9A2B24BD92E0C504");

            entity.HasIndex(e => new { e.ShopId, e.EndDate }, "IX_ShopSubscriptions_ShopID");

            entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ExtendedByAdminId).HasColumnName("ExtendedByAdminID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.PaymentMode).HasMaxLength(50);
            entity.Property(e => e.PaymentReference).HasMaxLength(100);
            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.ExtendedByAdmin).WithMany(p => p.ShopSubscriptions)
                .HasForeignKey(d => d.ExtendedByAdminId)
                .HasConstraintName("FK__ShopSubsc__Exten__619B8048");

            entity.HasOne(d => d.Plan).WithMany(p => p.ShopSubscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShopSubsc__PlanI__60A75C0F");

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopSubscriptions)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShopSubsc__ShopI__5FB337D6");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId).HasName("PK__StockMov__D1822466E8226293");

            entity.HasIndex(e => new { e.ShopId, e.ProductId }, "IX_StockMovements_ProductID");

            entity.Property(e => e.MovementId).HasColumnName("MovementID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.MovementType)
                .IsRequired()
                .HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.QuantityChange).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.ReferenceId).HasColumnName("ReferenceID");
            entity.Property(e => e.ReferenceType).HasMaxLength(30);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.StockAfter).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.StockBefore).HasColumnType("decimal(12, 3)");

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StockMove__Produ__531856C7");

            entity.HasOne(d => d.Shop).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StockMove__ShopI__5224328E");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__Subscrip__755C22D7F6211829");

            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PlanName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666942A50E8C5");

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.CompanyName)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.ContactPersonName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(20)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MobileNumber).HasMaxLength(15);
            entity.Property(e => e.OpeningBalance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");

            entity.HasOne(d => d.Shop).WithMany(p => p.Suppliers)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Suppliers__ShopI__71D1E811");
        });

        modelBuilder.Entity<SupplierPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Supplier__9B556A5864E63E8B");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.PaymentMode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Cash");
            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

            entity.HasOne(d => d.Purchase).WithMany(p => p.SupplierPayments)
                .HasForeignKey(d => d.PurchaseId)
                .HasConstraintName("FK__SupplierP__Purch__151B244E");

            entity.HasOne(d => d.Shop).WithMany(p => p.SupplierPayments)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SupplierP__ShopI__1332DBDC");

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplierPayments)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SupplierP__Suppl__14270015");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__Units__44F5EC9539C47EE1");

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
            entity.Property(e => e.TotalBilled).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalPaid).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalPending).HasColumnType("decimal(38, 2)");
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
            entity.Property(e => e.OpeningBalance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.OutstandingDue).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalPaid).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalPurchased).HasColumnType("decimal(38, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
