// ================================================
//  AgroBillling.API / Controllers / BillsController.cs
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/bills")]
    [Authorize(Roles = "SHOP")]
    public class BillsController : ControllerBase
    {
        private readonly IBillRepository _repo;
        private readonly IBillService    _billService;

        public BillsController(IBillRepository repo, IBillService billService)
        {
            _repo        = repo;
            _billService = billService;
        }

        [HttpGet("{shopId}")]
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
                Items      = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize   = pageSize
            });
        }

        [HttpGet("detail/{billId}")]
        public async Task<IActionResult> GetById(int billId)
        {
            var bill = await _repo.GetWithDetailsAsync(billId);
            if (bill == null) return NotFound(ApiResponse<string>.Fail("Bill not found"));
            return Ok(ApiResponse<Bill>.Ok(bill));
        }

        [HttpPost("{shopId}")]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateBillDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            if (!dto.Items.Any())
                return BadRequest(ApiResponse<string>.Fail("At least one item is required"));

            var bill = await _billService.CreateBillAsync(shopId, dto);
            return Ok(ApiResponse<Bill>.Ok(bill, "Bill created successfully"));
        }

        [HttpPost("payment")]
        public async Task<IActionResult> AddPayment([FromBody] AddPaymentDto dto)
        {
            var payment = await _billService.AddPaymentAsync(dto);
            return Ok(ApiResponse<BillPayment>.Ok(payment, "Payment recorded successfully"));
        }

        [HttpPost("{shopId}/return")]
        public async Task<IActionResult> ProcessReturn(int shopId, [FromBody] CreateReturnDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            if (!dto.Items.Any())
                return BadRequest(ApiResponse<string>.Fail("At least one item is required"));

            var creditNote = await _billService.ProcessReturnAsync(shopId, dto);
            return Ok(ApiResponse<CreditNote>.Ok(creditNote, "Return processed successfully"));
        }

        [HttpGet("{shopId}/credit-notes")]
        public async Task<IActionResult> GetCreditNotes(int shopId, [FromQuery] int? customerId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var notes = await _repo.GetByShopIdAsync(shopId);
            return Ok(ApiResponse<IEnumerable<Bill>>.Ok(notes));
        }

        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            return int.TryParse(claim, out var id) && id == shopId;
        }
    }
}
