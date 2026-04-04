// ================================================
//  AgroBilling.API / Controllers / PaymentsController.cs
//  ✅ FIXED - Same pattern as ProductsController
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly AgroBillingDbContext _context;

        public PaymentsController(AgroBillingDbContext context)
        {
            _context = context;
        }

        // ─── HELPER METHODS ───────────────────────────────────────

        private int? GetCurrentShopId()
        {
            var claim = User.FindFirst("shopId")?.Value
                ?? User.FindFirst("ShopId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(claim, out var shopId) ? shopId : null;
        }

        private string GetUserRole()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value
                    ?? User.FindFirst("Role")?.Value;

            return role ?? "";
        }

        private bool IsShopAuthorized(int shopId)
        {
            var role = GetUserRole();
            if (role == "ADMIN") return true;

            var currentShopId = GetCurrentShopId();
            if (!currentShopId.HasValue) return false;

            return currentShopId.Value == shopId;
        }

        private int GetShopId()
        {
            return GetCurrentShopId() ?? 0;
        }

        // ─── SHOP: Submit payment request ────────────────────────
        [HttpPost("request")]
        [Authorize]
        public async Task<IActionResult> SubmitRequest([FromBody] SubmitPaymentRequestDto dto)
        {
            var role = GetUserRole();
            if (role != "SHOP") return Forbid();

            var shopId = GetShopId();
            if (shopId == 0) return Unauthorized(ApiResponse<string>.Fail("Unauthorized"));

            var existing = await _context.PaymentRequests
                .FirstOrDefaultAsync(p => p.ShopId == shopId && p.Status == "PENDING");
            if (existing != null)
                return BadRequest(ApiResponse<string>.Fail("Aapki ek request already pending hai. Admin ke approve hone ka wait karo."));

            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p =>
                    (dto.PlanType == "monthly" && !p.IsTrial && p.DurationDays <= 31) ||
                    (dto.PlanType == "yearly" && !p.IsTrial && p.DurationDays > 31));

            if (plan == null)
                return BadRequest(ApiResponse<string>.Fail("Invalid plan selected"));

            var request = new PaymentRequest
            {
                ShopId = shopId,
                PlanId = plan.PlanId,
                Amount = dto.Amount,
                TransactionId = dto.TransactionId,
                PayerName = dto.PayerName,
                PayerMobile = dto.PayerMobile ?? "",
                Status = "PENDING",
                RequestedAt = DateTime.UtcNow
            };

            _context.PaymentRequests.Add(request);

            _context.AdminNotifications.Add(new AdminNotification
            {
                ShopId = shopId,
                NotificationType = "PAYMENT_REQUEST",
                Message = $"New payment request: ₹{dto.Amount} via UPI (TxnID: {dto.TransactionId})",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { requestId = request.RequestId }, "Payment request submitted. Admin verify karega."));
        }

        // ─── SHOP: Check current payment request status ──────────
        [HttpGet("shop-status")]
        [Authorize]
        public async Task<IActionResult> GetShopStatus()
        {
            var role = GetUserRole();
            if (role != "SHOP") return Forbid();

            var shopId = GetShopId();
            if (shopId == 0) return Unauthorized();

            var request = await _context.PaymentRequests
                .OrderByDescending(p => p.RequestedAt)
                .FirstOrDefaultAsync(p => p.ShopId == shopId);

            if (request == null)
                return Ok(ApiResponse<object>.Ok(null, "No request found"));

            return Ok(ApiResponse<object>.Ok(new
            {
                status = request.Status,
                transactionId = request.TransactionId,
                submittedAt = request.RequestedAt,
                adminNotes = request.AdminNotes
            }));
        }

        // ─── ADMIN: Get all pending requests ─────────────────────
        [HttpGet("pending")]
        [Authorize]
        public async Task<IActionResult> GetPending()
        {
            var role = GetUserRole();
            if (role != "ADMIN") return Forbid();

            var requests = await _context.PaymentRequests
                .Include(p => p.Shop)
                .Include(p => p.Plan)
                .Where(p => p.Status == "PENDING")
                .OrderBy(p => p.RequestedAt)
                .Select(p => new
                {
                    requestId = p.RequestId,
                    shopId = p.ShopId,
                    shopName = p.Shop.ShopName,
                    ownerName = p.Shop.OwnerName,
                    mobileNumber = p.Shop.MobileNumber,
                    planType = p.Plan.PlanName,
                    amount = p.Amount,
                    transactionId = p.TransactionId,
                    payerName = p.PayerName,
                    payerMobile = p.PayerMobile,
                    status = p.Status,
                    createdAt = p.RequestedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(requests));
        }

        // ─── ADMIN: Approve or Reject ─────────────────────────────
        [HttpPost("{requestId}/review")]
        [Authorize]
        public async Task<IActionResult> Review(int requestId, [FromBody] ReviewPaymentRequestDto dto)
        {
            var role = GetUserRole();
            if (role != "ADMIN") return Forbid();

            var request = await _context.PaymentRequests
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.RequestId == requestId);

            if (request == null)
                return NotFound(ApiResponse<string>.Fail("Request not found"));

            if (request.Status != "PENDING")
                return BadRequest(ApiResponse<string>.Fail("Request already reviewed"));

            var adminClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(adminClaim, out var adminId);

            request.Status = dto.Action == "APPROVE" ? "APPROVED" : "REJECTED";
            request.AdminNotes = dto.AdminNotes;
            request.ApprovedAt = DateTime.UtcNow;
            request.ApprovedByAdminId = adminId > 0 ? adminId : null;

            if (dto.Action == "APPROVE")
            {
                var oldSubs = await _context.ShopSubscriptions
                    .Where(s => s.ShopId == request.ShopId && s.IsActive)
                    .ToListAsync();
                oldSubs.ForEach(s => s.IsActive = false);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                _context.ShopSubscriptions.Add(new ShopSubscription
                {
                    ShopId = request.ShopId,
                    PlanId = request.PlanId,
                    StartDate = today,
                    EndDate = today.AddDays(request.Plan.DurationDays),
                    AmountPaid = request.Amount,
                    PaymentMode = "UPI",
                    PaymentReference = request.TransactionId,
                    IsActive = true,
                    ExtendedByAdminId = adminId > 0 ? adminId : null,
                    Notes = $"Payment approved. TxnID: {request.TransactionId}",
                    CreatedAt = DateTime.UtcNow
                });

                _context.AdminNotifications.Add(new AdminNotification
                {
                    ShopId = request.ShopId,
                    NotificationType = "PAYMENT_APPROVED",
                    Message = $"Aapki payment approve ho gayi! Subscription: {request.Plan.PlanName}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                _context.AdminNotifications.Add(new AdminNotification
                {
                    ShopId = request.ShopId,
                    NotificationType = "PAYMENT_REJECTED",
                    Message = $"Payment request reject hui. Reason: {dto.AdminNotes ?? "Invalid transaction"}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("ok", $"Request {request.Status.ToLower()} successfully"));
        }
    }

    // ─── Local DTOs ────────────────────────────────────────────────
    public class SubmitPaymentRequestDto
    {
        public int ShopId { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string PayerName { get; set; } = string.Empty;
        public string? PayerMobile { get; set; }
    }

    public class ReviewPaymentRequestDto
    {
        public string Action { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
    }
}