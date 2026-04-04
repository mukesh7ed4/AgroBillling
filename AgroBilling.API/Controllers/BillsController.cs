// ================================================
//  AgroBillling.API / Controllers / BillsController.cs
//  ✅ FIXED - Same pattern as ProductsController
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/bills")]
    public class BillsController : ControllerBase
    {
        private readonly IBillRepository _repo;
        private readonly IBillService _billService;

        public BillsController(IBillRepository repo, IBillService billService)
        {
            _repo = repo;
            _billService = billService;
        }

        // ─── HELPER METHODS ───────────────────────────────────────

        private int? GetCurrentShopId()
        {
            var claim = User.FindFirst("shopId")?.Value
                ?? User.FindFirst("ShopId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(claim, out var shopId))
                return shopId;

            return null;
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
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var items = await _repo.GetByShopIdAsync(shopId, search, status, page, pageSize);
            var total = await _repo.GetCountAsync(shopId, status);

            return Ok(new PagedResponse<Bill>
            {
                Items = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        [HttpGet("detail/{billId}")]
        [Authorize]
        public async Task<IActionResult> GetById(int billId)
        {
            var bill = await _repo.GetWithDetailsAsync(billId);
            if (bill == null) return NotFound(ApiResponse<string>.Fail("Bill not found"));

            if (!IsShopAuthorized(bill.ShopId)) return Forbid();

            return Ok(ApiResponse<Bill>.Ok(bill));
        }

        [HttpPost("{shopId}")]
        [Authorize]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateBillDto dto)
        {
            Console.WriteLine($"=== CREATE BILL CALLED ===");
            Console.WriteLine($"ShopId: {shopId}");
            Console.WriteLine($"Role: {GetUserRole()}");

            if (!IsShopAuthorized(shopId)) return Forbid();

            if (dto == null || !dto.Items.Any())
                return BadRequest(ApiResponse<string>.Fail("At least one item is required"));

            var bill = await _billService.CreateBillAsync(shopId, dto);
            return Ok(ApiResponse<Bill>.Ok(bill, "Bill created successfully"));
        }

        [HttpPost("{shopId}/bulk-payment")]
        [Authorize]
        public async Task<IActionResult> BulkPayment(int shopId, [FromBody] BulkPaymentDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var result = await _billService.BulkPaymentAsync(shopId, dto);
            return Ok(ApiResponse<BulkPaymentResultDto>.Ok(result, "Payment distributed successfully"));
        }

        [HttpPost("payment")]
        [Authorize]
        public async Task<IActionResult> AddPayment([FromBody] AddPaymentDto dto)
        {
            var payment = await _billService.AddPaymentAsync(dto);
            return Ok(ApiResponse<BillPayment>.Ok(payment, "Payment recorded successfully"));
        }

        [HttpPost("{shopId}/return")]
        [Authorize]
        public async Task<IActionResult> ProcessReturn(int shopId, [FromBody] CreateReturnDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            if (!dto.Items.Any())
                return BadRequest(ApiResponse<string>.Fail("At least one item is required"));

            var creditNote = await _billService.ProcessReturnAsync(shopId, dto);
            return Ok(ApiResponse<CreditNote>.Ok(creditNote, "Return processed successfully"));
        }

        [HttpGet("{shopId}/credit-notes")]
        [Authorize]
        public async Task<IActionResult> GetCreditNotes(int shopId, [FromQuery] int? customerId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var notes = await _repo.GetByShopIdAsync(shopId);
            return Ok(ApiResponse<IEnumerable<Bill>>.Ok(notes));
        }
    }
}