// ================================================
//  AgroBilling.API / Controllers / SuppliersController.cs
//  ✅ FIXED - Same pattern as ProductsController
// ================================================

using AgroBilling.DAL.Models;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgroBilling.API.Controllers
{
    [ApiController]
    [Route("api/suppliers")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierRepository _repo;
        private readonly IPurchaseRepository _purchaseRepo;

        public SuppliersController(ISupplierRepository repo, IPurchaseRepository purchaseRepo)
        {
            _repo = repo;
            _purchaseRepo = purchaseRepo;
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
        public async Task<IActionResult> GetAll(int shopId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var suppliers = await _repo.GetByShopIdAsync(shopId);
            return Ok(ApiResponse<IEnumerable<Supplier>>.Ok(suppliers));
        }

        [HttpGet("{supplierId}/ledger")]
        [Authorize]
        public async Task<IActionResult> GetLedger(int supplierId)
        {
            var supplier = await _repo.GetByIdAsync(supplierId);
            if (supplier == null)
                return NotFound(ApiResponse<string>.Fail("Supplier not found"));

            if (!IsShopAuthorized(supplier.ShopId))
                return Forbid();

            var ledger = await _repo.GetLedgerAsync(supplierId);
            return Ok(ApiResponse<SupplierLedgerDto>.Ok(ledger));
        }

        [HttpPost("{shopId}")]
        [Authorize]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateSupplierDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var supplier = new Supplier
            {
                ShopId = shopId,
                CompanyName = dto.CompanyName,
                ContactPersonName = dto.ContactPersonName,
                MobileNumber = dto.MobileNumber,
                Email = dto.Email,
                Address = dto.Address,
                Gstnumber = dto.GstNumber ?? "",
                OpeningBalance = dto.OpeningBalance,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(supplier);
            return Ok(ApiResponse<Supplier>.Ok(supplier, "Supplier added successfully"));
        }

        [HttpPut("{supplierId}")]
        [Authorize]
        public async Task<IActionResult> Update(int supplierId, [FromBody] Supplier dto)
        {
            var supplier = await _repo.GetByIdAsync(supplierId);
            if (supplier == null)
                return NotFound(ApiResponse<string>.Fail("Supplier not found"));

            if (!IsShopAuthorized(supplier.ShopId))
                return Forbid();

            supplier.CompanyName = dto.CompanyName;
            supplier.ContactPersonName = dto.ContactPersonName;
            supplier.MobileNumber = dto.MobileNumber;
            supplier.Email = dto.Email;
            supplier.Address = dto.Address;
            supplier.Gstnumber = dto.Gstnumber ?? "";

            await _repo.UpdateAsync(supplier);
            return Ok(ApiResponse<Supplier>.Ok(supplier));
        }

        [HttpPost("{supplierId}/payment")]
        [Authorize]
        public async Task<IActionResult> AddPayment(int supplierId, [FromBody] AddSupplierPaymentDto dto)
        {
            var supplier = await _repo.GetByIdAsync(supplierId);
            if (supplier == null)
                return NotFound(ApiResponse<string>.Fail("Supplier not found"));

            if (!IsShopAuthorized(supplier.ShopId))
                return Forbid();

            var payment = new SupplierPayment
            {
                ShopId = supplier.ShopId,
                SupplierId = supplierId,
                PurchaseId = dto.PurchaseId,
                PaymentDate = dto.PaymentDate,
                Amount = dto.Amount,
                PaymentMode = dto.PaymentMode,
                Reference = dto.Reference,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _purchaseRepo.AddSupplierPaymentAsync(payment);
            return Ok(ApiResponse<SupplierPayment>.Ok(payment, "Payment recorded"));
        }

        [HttpGet("debug/claims")]
        [Authorize]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(claims);
        }
    }
}