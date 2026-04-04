// ================================================
//  AgroBilling.API / Controllers / CustomersController.cs
//  ✅ FIXED — Allow both ADMIN and SHOP roles
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]  // ✅ Changed from [Authorize(Roles = "SHOP")] to allow both
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _repo;

        public CustomersController(ICustomerRepository repo)
        {
            _repo = repo;
        }

        // ✅ Helper to check if user is admin
        private bool IsAdmin()
        {
            var roleClaim = User.FindFirst("role")?.Value?.ToUpper();
            return roleClaim == "ADMIN";
        }

        // ✅ Helper to check shop authorization
        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            var roleClaim = User.FindFirst("role")?.Value?.ToUpper();

            // Admin can access any shop
            if (roleClaim == "ADMIN") return true;

            // Shop can only access their own data
            return int.TryParse(claim, out var id) && id == shopId;
        }

        // GET api/customers/{shopId}?search=&page=&pageSize=
        [HttpGet("{shopId:int}")]
        public async Task<IActionResult> GetAll(
            int shopId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var (items, total) = await _repo.GetPagedByShopIdAsync(shopId, search, page, pageSize);

            return Ok(new PagedResponse<Customer>
            {
                Items = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        // GET api/customers/detail/{customerId}
        [HttpGet("detail/{customerId:int}")]
        public async Task<IActionResult> GetById(int customerId)
        {
            var customer = await _repo.GetByIdAsync(customerId);
            if (customer == null)
                return NotFound(ApiResponse<string>.Fail("Customer not found"));

            // Check authorization
            if (!IsShopAuthorized(customer.ShopId)) return Forbid();

            return Ok(ApiResponse<Customer>.Ok(customer));
        }

        // GET api/customers/{customerId}/ledger
        [HttpGet("{customerId:int}/ledger")]
        public async Task<IActionResult> GetLedger(
            int customerId,
            [FromQuery] int billsTake = 50,
            [FromQuery] int paymentsTake = 100,
            [FromQuery] int creditsTake = 50)
        {
            var customer = await _repo.GetByIdAsync(customerId);
            if (customer == null)
                return NotFound(ApiResponse<string>.Fail("Customer not found"));

            if (!IsShopAuthorized(customer.ShopId)) return Forbid();

            billsTake = Math.Clamp(billsTake, 1, 200);
            paymentsTake = Math.Clamp(paymentsTake, 1, 300);
            creditsTake = Math.Clamp(creditsTake, 1, 200);

            var ledger = await _repo.GetLedgerAsync(
                customerId, billsTake, paymentsTake, creditsTake);
            return Ok(ApiResponse<CustomerLedgerDto>.Ok(ledger));
        }

        // POST api/customers/{shopId}
        [HttpPost("{shopId:int}")]
        public async Task<IActionResult> Create(
            int shopId, [FromBody] CreateCustomerDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest(ApiResponse<string>.Fail("Full name is required"));

            if (string.IsNullOrWhiteSpace(dto.MobileNumber))
                return BadRequest(ApiResponse<string>.Fail("Mobile number is required"));

            var customer = new Customer
            {
                ShopId = shopId,
                FullName = dto.FullName.Trim(),
                FatherName = dto.FatherName?.Trim(),
                MobileNumber = dto.MobileNumber.Trim(),
                AlternateMobile = dto.AlternateMobile?.Trim(),
                Village = dto.Village?.Trim(),
                Tehsil = dto.Tehsil?.Trim(),
                District = dto.District?.Trim(),
                State = dto.State?.Trim() ?? "Haryana",
                LandAcres = dto.LandAcres,
                OpeningBalance = dto.OpeningBalance,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(customer);
            return Ok(ApiResponse<Customer>.Ok(customer, "Customer added successfully"));
        }

        // PUT api/customers/{customerId}
        [HttpPut("{customerId:int}")]
        public async Task<IActionResult> Update(
            int customerId, [FromBody] CreateCustomerDto dto)
        {
            var customer = await _repo.GetByIdAsync(customerId);
            if (customer == null)
                return NotFound(ApiResponse<string>.Fail("Customer not found"));

            if (!IsShopAuthorized(customer.ShopId)) return Forbid();

            customer.FullName = dto.FullName.Trim();
            customer.FatherName = dto.FatherName?.Trim();
            customer.MobileNumber = dto.MobileNumber.Trim();
            customer.AlternateMobile = dto.AlternateMobile?.Trim();
            customer.Village = dto.Village?.Trim();
            customer.Tehsil = dto.Tehsil?.Trim();
            customer.District = dto.District?.Trim();
            customer.State = dto.State?.Trim() ?? "Haryana";
            customer.LandAcres = dto.LandAcres;

            await _repo.UpdateAsync(customer);
            return Ok(ApiResponse<Customer>.Ok(customer));
        }
    }
}