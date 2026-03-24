// ================================================
//  AgroBillling.API / Controllers / AdminController.cs
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using AgroBillling.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using AgroBillling.DAL.Context;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly IShopRepository          _shopRepo;
        private readonly ISubscriptionRepository  _subRepo;
        private readonly INotificationRepository  _notifRepo;
        private readonly IReportRepository        _reportRepo;
        private readonly AgroBillingDbContext     _context;

        public AdminController(
            IShopRepository         shopRepo,
            ISubscriptionRepository subRepo,
            INotificationRepository notifRepo,
            IReportRepository       reportRepo,
            AgroBillingDbContext    context)
        {
            _shopRepo   = shopRepo;
            _subRepo    = subRepo;
            _notifRepo  = notifRepo;
            _reportRepo = reportRepo;
            _context    = context;
        }

        // ─── DASHBOARD ───
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var data = await _reportRepo.GetAdminDashboardAsync();
            return Ok(ApiResponse<AdminDashboardDto>.Ok(data));
        }

        // ─── SHOPS ───
        [HttpGet("shops")]
        public async Task<IActionResult> GetShops([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (items, total) = await _shopRepo.GetPagedWithSubscriptionsAsync(search, page, pageSize);

            return Ok(new PagedResponse<Shop>
            {
                Items      = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize   = pageSize
            });
        }

        [HttpPost("shops")]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopDto dto)
        {
            var passwordHash = HashPassword(dto.Password);
            var shop = new Shop
            {
                OwnerName           = dto.OwnerName,
                ShopName            = dto.ShopName,
                MobileNumber        = dto.MobileNumber,
                AlternateMobile     = dto.AlternateMobile,
                Email               = dto.Email,
                Address             = dto.Address,
                City                = dto.City,
                State               = dto.State,
                PinCode             = dto.PinCode,
                Gstnumber           = dto.GstNumber ?? "",
                Gstpercent          = dto.GstPercent,
                BillStartNumber     = dto.BillStartNumber,
                CurrentBillSequence = 0,
                PasswordHash        = passwordHash,
                IsActive            = true,
                CreatedAt           = DateTime.Now,
                CreatedByAdminId    = GetAdminId()
            };

            await _shopRepo.AddAsync(shop);

            // Create subscription
            var plan = await _context.SubscriptionPlans.FindAsync(dto.PlanId);
            if (plan != null)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                await _subRepo.AddAsync(new ShopSubscription
                {
                    ShopId    = shop.ShopId,
                    PlanId    = dto.PlanId,
                    StartDate = today,
                    EndDate   = today.AddDays(plan.DurationDays),
                    AmountPaid= 0,
                    IsActive  = true,
                    CreatedAt = DateTime.Now
                });

                // Notification
                await _notifRepo.AddAsync(new AdminNotification
                {
                    ShopId           = shop.ShopId,
                    NotificationType = "NEW_SIGNUP",
                    Message          = $"New shop registered: {shop.ShopName} ({shop.OwnerName})",
                    IsRead           = false,
                    CreatedAt        = DateTime.Now
                });
            }

            return Ok(ApiResponse<Shop>.Ok(shop, "Shop registered successfully"));
        }

        // ─── SUBSCRIPTION ───
        [HttpPost("shops/{shopId}/extend")]
        public async Task<IActionResult> ExtendSubscription(int shopId, [FromBody] ExtendSubscriptionDto dto)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(dto.PlanId);
            if (plan == null)
                return BadRequest(ApiResponse<string>.Fail("Plan not found"));

            // Deactivate current
            await _subRepo.DeactivateAllByShopIdAsync(shopId);

            var today = DateOnly.FromDateTime(DateTime.Now);
            var sub   = new ShopSubscription
            {
                ShopId           = shopId,
                PlanId           = dto.PlanId,
                StartDate        = today,
                EndDate          = today.AddDays(plan.DurationDays),
                AmountPaid       = dto.AmountPaid,
                PaymentMode      = dto.PaymentMode,
                PaymentReference = dto.PaymentRef,
                IsActive         = true,
                ExtendedByAdminId= GetAdminId(),
                Notes            = dto.Notes,
                CreatedAt        = DateTime.Now
            };

            await _subRepo.AddAsync(sub);

            // Mark old notifications read
            await _notifRepo.MarkAllReadAsync();

            return Ok(ApiResponse<ShopSubscription>.Ok(sub, "Subscription extended"));
        }

        // ─── PLANS ───
        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _context.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.Price)
                .ToListAsync();
            return Ok(ApiResponse<List<SubscriptionPlan>>.Ok(plans));
        }

        // ─── NOTIFICATIONS ───
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var notifs = await _notifRepo.GetUnreadAsync();
            return Ok(ApiResponse<IEnumerable<AdminNotification>>.Ok(notifs));
        }

        [HttpPatch("notifications/{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notifRepo.MarkReadAsync(id);
            return Ok(ApiResponse<string>.Ok("ok"));
        }

        // ─── HELPERS ───
        private int GetAdminId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 1;
        }

        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
