// ================================================
//  AGROBILLING — ALL REPOSITORY INTERFACES
//  AgroBilling.DAL / Repositories / Interfaces
// ================================================

using AgroBillling.DAL.Models;

namespace AgroBillling.DAL.Repositories.Interfaces
{
    // ─── GENERIC BASE ───
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }

    // ─── SHOP ───
    public interface IShopRepository : IGenericRepository<Shop>
    {
        Task<Shop?> GetByEmailAsync(string email);
        Task<IEnumerable<Shop>> GetAllWithSubscriptionsAsync();
        /// <summary>Search + paging in the database (avoids loading all shops into memory).</summary>
        Task<(IReadOnlyList<Shop> Items, int TotalCount)> GetPagedWithSubscriptionsAsync(string? search, int page, int pageSize);
        Task<int> GetNextBillNumberAsync(int shopId);
    }

    // ─── SUBSCRIPTION ───
    public interface ISubscriptionRepository : IGenericRepository<ShopSubscription>
    {
        Task<ShopSubscription?> GetActiveByShopIdAsync(int shopId);
        Task<IEnumerable<ShopSubscription>> GetExpiringSoonAsync(int days);
        Task<IEnumerable<ShopSubscription>> GetExpiredAsync();
        Task DeactivateAllByShopIdAsync(int shopId);
    }

    // ─── CUSTOMER ───
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        Task<IEnumerable<Customer>> GetByShopIdAsync(int shopId, string? search = null);
        Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedByShopIdAsync(
            int shopId, string? search, int page, int pageSize);
        Task<Customer?> GetByMobileAsync(int shopId, string mobile);
        Task<CustomerLedgerDto> GetLedgerAsync(int customerId, int billsTake = 50, int paymentsTake = 100, int creditsTake = 50);
    }

    // ─── PRODUCT ───
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetByShopIdAsync(int shopId, string? search = null, int? categoryId = null);
        Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedByShopIdAsync(
            int shopId, string? search, int? categoryId, int page, int pageSize);
        Task<IEnumerable<Product>> GetLowStockAsync(int shopId);
        Task UpdateStockAsync(int productId, decimal quantityChange);
        void InvalidateProductCache(int shopId);
    }

    // ─── SUPPLIER ───
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        Task<IEnumerable<Supplier>> GetByShopIdAsync(int shopId);
        Task<SupplierLedgerDto> GetLedgerAsync(int supplierId);
    }

    // ─── BILL ───
    public interface IBillRepository : IGenericRepository<Bill>
    {
        Task<IEnumerable<Bill>> GetByShopIdAsync(int shopId, string? search = null, string? status = null, int page = 1, int pageSize = 20);
        Task<int> GetCountAsync(int shopId, string? status = null);
        Task<Bill?> GetWithDetailsAsync(int billId);
        Task<IEnumerable<Bill>> GetByCustomerIdAsync(int customerId);
        Task AddPaymentAsync(BillPayment payment);
        Task UpdatePaymentStatusAsync(int billId);
    }

    // ─── PURCHASE ───
    public interface IPurchaseRepository : IGenericRepository<PurchaseOrder>
    {
        Task<IEnumerable<PurchaseOrder>> GetByShopIdAsync(int shopId, int page = 1, int pageSize = 20);
        Task<int> GetCountAsync(int shopId);
        Task<PurchaseOrder?> GetWithItemsAsync(int purchaseId);
        Task AddSupplierPaymentAsync(SupplierPayment payment);
    }

    // ─── EXPENSE ───
    public interface IExpenseRepository : IGenericRepository<Expense>
    {
        Task<IEnumerable<Expense>> GetByShopIdAsync(int shopId, int year, int month);
        Task<(IReadOnlyList<Expense> Items, int TotalCount, decimal MonthTotal)> GetPagedForMonthAsync(
            int shopId, int year, int month, int page, int pageSize);
        Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync(int? shopId = null);
    }

    // ─── CREDIT NOTE ───
    public interface ICreditNoteRepository : IGenericRepository<CreditNote>
    {
        Task<IEnumerable<CreditNote>> GetByCustomerIdAsync(int customerId);
    }

    // ─── REPORT ───
    public interface IReportRepository
    {
        Task<MonthlyDashboardDto> GetMonthlyDashboardAsync(int shopId, int year, int month);
        Task<AdminDashboardDto> GetAdminDashboardAsync();
    }

    // ─── NOTIFICATION ───
    public interface INotificationRepository : IGenericRepository<AdminNotification>
    {
        Task<IEnumerable<AdminNotification>> GetUnreadAsync();
        Task MarkReadAsync(int notificationId);
        Task MarkAllReadAsync();
    }

    // ─── AUTH ───
    public interface IAuthRepository
    {
        Task<Shop?> ValidateShopAsync(string email, string passwordHash);
        Task<AdminUser?> ValidateAdminAsync(string email, string passwordHash);
    }
}
