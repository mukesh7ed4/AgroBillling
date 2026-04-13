// ================================================
//  AgroBilling.API / Controllers / PurchasesController.cs
//  ✅ COMPLETE FIXED VERSION
// ================================================

using AgroBilling.DAL.Models;
using AgroBilling.DAL.Repositories;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgroBilling.API.Controllers
{
    [ApiController]
    [Route("api/purchases")]
    public class PurchasesController : ControllerBase
    {
        private readonly IPurchaseRepository _repo;
        private readonly IPurchaseService _purchaseService;

        public PurchasesController(IPurchaseRepository repo, IPurchaseService purchaseService)
        {
            _repo = repo;
            _purchaseService = purchaseService;
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

        // ─── ENDPOINTS ───────────────────────────────────────────

        [HttpGet("{shopId}")]
        [Authorize]
        public async Task<IActionResult> GetAll(int shopId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            Console.WriteLine($"=== GET PURCHASES CALLED ===");
            Console.WriteLine($"ShopId: {shopId}");
            Console.WriteLine($"Role: {GetUserRole()}");

            if (!IsShopAuthorized(shopId)) return Forbid();

            var items = await _repo.GetByShopIdAsync(shopId, page, pageSize);
            var total = await _repo.GetCountAsync(shopId);

            return Ok(new PagedResponse<PurchaseOrder>
            {
                Items = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        [HttpGet("detail/{purchaseId}")]
        [Authorize]
        public async Task<IActionResult> GetById(int purchaseId)
        {
            var purchase = await _repo.GetWithItemsAsync(purchaseId);
            if (purchase == null) return NotFound(ApiResponse<string>.Fail("Purchase not found"));

            if (!IsShopAuthorized(purchase.ShopId)) return Forbid();

            return Ok(ApiResponse<PurchaseOrder>.Ok(purchase));
        }

        [HttpPost("{shopId}")]
        [Authorize]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreatePurchaseDto dto)
        {
            Console.WriteLine($"=== CREATE PURCHASE CALLED ===");
            Console.WriteLine($"ShopId: {shopId}");
            Console.WriteLine($"Role: {GetUserRole()}");

            if (!IsShopAuthorized(shopId)) return Forbid();

            if (dto == null || !dto.Items.Any())
                return BadRequest(ApiResponse<string>.Fail("At least one item is required"));

            var purchase = await _purchaseService.CreatePurchaseAsync(shopId, dto);
            return Ok(ApiResponse<PurchaseOrder>.Ok(purchase, "Purchase order created"));
        }
    }
}