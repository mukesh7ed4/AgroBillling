// ================================================
//  AgroBillling.API / Controllers / PurchasesController.cs
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/purchases")]
    [Authorize(Roles = "SHOP")]
    public class PurchasesController : ControllerBase
    {
        private readonly IPurchaseRepository _repo;
        private readonly IPurchaseService    _purchaseService;

        public PurchasesController(IPurchaseRepository repo, IPurchaseService purchaseService)
        {
            _repo            = repo;
            _purchaseService = purchaseService;
        }

        [HttpGet("{shopId}")]
        public async Task<IActionResult> GetAll(int shopId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var items = await _repo.GetByShopIdAsync(shopId, page, pageSize);
            var total = await _repo.GetCountAsync(shopId);

            return Ok(new PagedResponse<PurchaseOrder>
            {
                Items      = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize   = pageSize
            });
        }

        [HttpGet("detail/{purchaseId}")]
        public async Task<IActionResult> GetById(int purchaseId)
        {
            var purchase = await _repo.GetWithItemsAsync(purchaseId);
            if (purchase == null) return NotFound(ApiResponse<string>.Fail("Purchase not found"));
            return Ok(ApiResponse<PurchaseOrder>.Ok(purchase));
        }

        [HttpPost("{shopId}")]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreatePurchaseDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            if (!dto.Items.Any())
                return BadRequest(ApiResponse<string>.Fail("At least one item is required"));

            var purchase = await _purchaseService.CreatePurchaseAsync(shopId, dto);
            return Ok(ApiResponse<PurchaseOrder>.Ok(purchase, "Purchase order created"));
        }

        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            return int.TryParse(claim, out var id) && id == shopId;
        }
    }
}
