// ================================================
//  AgroBillling.API / Controllers / SuppliersController.cs
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/suppliers")]
    [Authorize(Roles = "SHOP")]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierRepository  _repo;
        private readonly IPurchaseRepository  _purchaseRepo;

        public SuppliersController(ISupplierRepository repo, IPurchaseRepository purchaseRepo)
        {
            _repo         = repo;
            _purchaseRepo = purchaseRepo;
        }

        [HttpGet("{shopId}")]
        public async Task<IActionResult> GetAll(int shopId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var suppliers = await _repo.GetByShopIdAsync(shopId);
            return Ok(ApiResponse<IEnumerable<Supplier>>.Ok(suppliers));
        }

        [HttpGet("{supplierId}/ledger")]
        public async Task<IActionResult> GetLedger(int supplierId)
        {
            var ledger = await _repo.GetLedgerAsync(supplierId);
            return Ok(ApiResponse<SupplierLedgerDto>.Ok(ledger));
        }

        [HttpPost("{shopId}")]
        public async Task<IActionResult> Create(int shopId, [FromBody] Supplier dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            dto.ShopId    = shopId;
            dto.IsActive  = true;
            dto.CreatedAt = DateTime.Now;

            await _repo.AddAsync(dto);
            return Ok(ApiResponse<Supplier>.Ok(dto, "Supplier added successfully"));
        }

        [HttpPut("{supplierId}")]
        public async Task<IActionResult> Update(int supplierId, [FromBody] Supplier dto)
        {
            var supplier = await _repo.GetByIdAsync(supplierId);
            if (supplier == null) return NotFound(ApiResponse<string>.Fail("Supplier not found"));

            supplier.CompanyName       = dto.CompanyName;
            supplier.ContactPersonName = dto.ContactPersonName;
            supplier.MobileNumber      = dto.MobileNumber;
            supplier.Email             = dto.Email;
            supplier.Address           = dto.Address;
            supplier.Gstnumber         = dto.Gstnumber ?? "";

            await _repo.UpdateAsync(supplier);
            return Ok(ApiResponse<Supplier>.Ok(supplier));
        }

        [HttpPost("{supplierId}/payment")]
        public async Task<IActionResult> AddPayment(int supplierId, [FromBody] AddSupplierPaymentDto dto)
        {
            var supplier = await _repo.GetByIdAsync(supplierId);
            if (supplier == null) return NotFound(ApiResponse<string>.Fail("Supplier not found"));

            var payment = new SupplierPayment
            {
                ShopId      = supplier.ShopId,
                SupplierId  = supplierId,
                PurchaseId  = dto.PurchaseId,
                PaymentDate = dto.PaymentDate,
                Amount      = dto.Amount,
                PaymentMode = dto.PaymentMode,
                Reference   = dto.Reference,
                Notes       = dto.Notes,
                CreatedAt   = DateTime.Now
            };

            await _purchaseRepo.AddSupplierPaymentAsync(payment);
            return Ok(ApiResponse<SupplierPayment>.Ok(payment, "Payment recorded"));
        }

        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            return int.TryParse(claim, out var id) && id == shopId;
        }
    }
}
