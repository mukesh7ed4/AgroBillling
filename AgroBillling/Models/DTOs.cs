// ================================================
//  AGROBILLING — ALL DTOs
//  AgroBilling.DAL / Models / DTOs.cs
// ================================================

namespace AgroBillling.DAL.Models
{
    // ─── AUTH ───
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? ShopId { get; set; }
        public string? ShopName { get; set; }
        public string? OwnerName { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? SubscriptionStatus { get; set; }
        public string? SubscriptionExpiry { get; set; }
    }

    // ─── GENERIC API RESPONSE ───
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string message) =>
            new() { Success = false, Message = message };
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        /// <summary>Optional aggregate for the full filtered set (e.g. month total for expenses).</summary>
        public decimal? MonthTotal { get; set; }
    }

    // ─── SHOP DTOs ───
    public class CreateShopDto
    {
        public string OwnerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = "Haryana";
        public string? PinCode { get; set; }
        public string? GstNumber { get; set; }
        public decimal GstPercent { get; set; } = 18;
        public int BillStartNumber { get; set; } = 1;
        public string Password { get; set; } = string.Empty;
        public int PlanId { get; set; }
    }

    public class ExtendSubscriptionDto
    {
        public int PlanId { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string? PaymentRef { get; set; }
        public string? Notes { get; set; }
    }

    // ─── CUSTOMER DTOs ───
    public class CreateCustomerDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public string? Village { get; set; }
        public string? Tehsil { get; set; }
        public string? District { get; set; }
        public string State { get; set; } = "Haryana";
        public decimal? LandAcres { get; set; }
        public decimal OpeningBalance { get; set; } = 0;
    }

    public class CustomerLedgerDto
    {
        public Customer? Customer { get; set; }
        public List<Bill> Bills { get; set; } = new();
        public List<BillPayment> Payments { get; set; } = new();
        public List<CreditNote> CreditNotes { get; set; } = new();

        /// <summary>Total bills for this customer (full count; Bills may be capped).</summary>
        public int BillsTotalCount { get; set; }
        public int PaymentsTotalCount { get; set; }
        public int CreditNotesTotalCount { get; set; }
        /// <summary>Sum of bill totals — correct even when Bills list is paged.</summary>
        public decimal LedgerTotalBilled { get; set; }
        /// <summary>Sum of amount paid on bills.</summary>
        public decimal LedgerTotalPaidOnBills { get; set; }
    }

    // ─── PRODUCT DTOs ───
    public class CreateProductDto
    {
        public int CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? HsnCode { get; set; }
        public int UnitId { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal GstPercent { get; set; } = 0;
        public bool UseShopGst { get; set; } = true;
        public decimal CurrentStock { get; set; } = 0;
        public decimal MinStockAlert { get; set; } = 5;
    }

    // ─── BILL DTOs ───
    public class CreateBillDto
    {
        public int CustomerId { get; set; }
        public DateOnly BillDate { get; set; }
        public decimal GstPercent { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal AmountPaid { get; set; } = 0;
        public string? Notes { get; set; }
        public List<BillItemDto> Items { get; set; } = new();
    }

    public class BillItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal GstPercent { get; set; } = 0;
        public decimal GstAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
    }

    public class AddPaymentDto
    {
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateOnly PaymentDate { get; set; }
    }

    public class CreateReturnDto
    {
        public int CustomerId { get; set; }
        public int OriginalBillId { get; set; }
        public DateOnly ReturnDate { get; set; }
        public string? Notes { get; set; }
        public List<ReturnItemDto> Items { get; set; } = new();
    }

    public class ReturnItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // ─── PURCHASE DTOs ───
    public class CreatePurchaseDto
    {
        public int SupplierId { get; set; }
        public DateOnly PurchaseDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal AmountPaid { get; set; } = 0;
        public string PaymentMode { get; set; } = "Cash";
        public string? Notes { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    public class PurchaseItemDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal GstPercent { get; set; } = 0;
        public decimal GstAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
    }

    public class AddSupplierPaymentDto
    {
        public int? PurchaseId { get; set; }
        public DateOnly PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    public class SupplierLedgerDto
    {
        public Supplier? Supplier { get; set; }
        public List<PurchaseOrder> Purchases { get; set; } = new();
        public List<SupplierPayment> Payments { get; set; } = new();
    }

    // ─── EXPENSE DTOs ───
    public class CreateExpenseDto
    {
        public int CategoryId { get; set; }
        public DateOnly ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string? Reference { get; set; }
    }

    // ─── DASHBOARD / REPORT DTOs ───
    public class ShopDashboardStatsDto
    {
        public int TotalBills { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPending { get; set; }
        public int PaidBills { get; set; }
        public int PartialBills { get; set; }
        public int PendingBills { get; set; }
    }

    public class ExpenseSummaryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalExpense { get; set; }
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class MonthlyDashboardDto
    {
        public ShopDashboardStatsDto SalesSummary { get; set; } = new();
        public List<ExpenseSummaryDto> ExpenseBreakdown { get; set; } = new();
        public decimal TotalExpenses { get; set; }
        public decimal TotalPurchased { get; set; }
        public decimal PaidToSuppliers { get; set; }
        public List<TopProductDto> TopProducts { get; set; } = new();
    }

    public class ShopAlertDto
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public DateOnly EndDate { get; set; }
        public int DaysLeft { get; set; }
    }

    public class ShopSummaryDto
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int DaysLeft { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AdminDashboardDto
    {
        public List<ShopAlertDto> ExpiringSoon { get; set; } = new();
        public List<ShopAlertDto> Expired { get; set; } = new();
        public List<ShopSummaryDto> AllShops { get; set; } = new();
    }

    public class SignupDto
    {
        public string ShopName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
