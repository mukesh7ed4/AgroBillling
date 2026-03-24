// ================================================
//  AgroBillling.API / Controllers / CustomersController.cs
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize(Roles = "SHOP")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _repo;

        public CustomersController(ICustomerRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("{shopId}")]
        public async Task<IActionResult> GetAll(int shopId, [FromQuery] string? search,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var (items, total) = await _repo.GetPagedByShopIdAsync(shopId, search, page, pageSize);

            return Ok(new PagedResponse<Customer>
            {
                Items      = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize   = pageSize
            });
        }

        [HttpGet("detail/{customerId}")]
        public async Task<IActionResult> GetById(int customerId)
        {
            var customer = await _repo.GetByIdAsync(customerId);
            if (customer == null) return NotFound(ApiResponse<string>.Fail("Customer not found"));
            return Ok(ApiResponse<Customer>.Ok(customer));
        }

        [HttpGet("{customerId}/ledger")]
        public async Task<IActionResult> GetLedger(
            int customerId,
            [FromQuery] int billsTake = 50,
            [FromQuery] int paymentsTake = 100,
            [FromQuery] int creditsTake = 50)
        {
            billsTake = Math.Clamp(billsTake, 1, 200);
            paymentsTake = Math.Clamp(paymentsTake, 1, 300);
            creditsTake = Math.Clamp(creditsTake, 1, 200);

            var ledger = await _repo.GetLedgerAsync(customerId, billsTake, paymentsTake, creditsTake);
            return Ok(ApiResponse<CustomerLedgerDto>.Ok(ledger));
        }

        [HttpPost("{shopId}")]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateCustomerDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var customer = new Customer
            {
                ShopId          = shopId,
                FullName        = dto.FullName,
                FatherName      = dto.FatherName,
                MobileNumber    = dto.MobileNumber,
                AlternateMobile = dto.AlternateMobile,
                Village         = dto.Village,
                Tehsil          = dto.Tehsil,
                District        = dto.District,
                State           = dto.State,
                LandAcres       = dto.LandAcres,
                OpeningBalance  = dto.OpeningBalance,
                IsActive        = true,
                CreatedAt       = DateTime.Now
            };

            await _repo.AddAsync(customer);
            return Ok(ApiResponse<Customer>.Ok(customer, "Customer added successfully"));
        }

        [HttpPut("{customerId}")]
        public async Task<IActionResult> Update(int customerId, [FromBody] CreateCustomerDto dto)
        {
            var customer = await _repo.GetByIdAsync(customerId);
            if (customer == null) return NotFound(ApiResponse<string>.Fail("Customer not found"));

            customer.FullName        = dto.FullName;
            customer.FatherName      = dto.FatherName;
            customer.MobileNumber    = dto.MobileNumber;
            customer.AlternateMobile = dto.AlternateMobile;
            customer.Village         = dto.Village;
            customer.Tehsil          = dto.Tehsil;
            customer.District        = dto.District;
            customer.State           = dto.State;
            customer.LandAcres       = dto.LandAcres;

            await _repo.UpdateAsync(customer);
            return Ok(ApiResponse<Customer>.Ok(customer));
        }

        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            return int.TryParse(claim, out var id) && id == shopId;
        }
    }
}
