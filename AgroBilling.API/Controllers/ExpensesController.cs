// ================================================
//  AgroBilling.API / Controllers / ExpensesController.cs
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
    [Route("api/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseRepository _repo;

        public ExpensesController(IExpenseRepository repo)
        {
            _repo = repo;
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
            [FromQuery] string? month,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            int year = DateTime.Now.Year;
            int monthNum = DateTime.Now.Month;

            if (!string.IsNullOrWhiteSpace(month) && month.Length == 7)
            {
                var parts = month.Split('-');
                if (parts.Length == 2)
                {
                    int.TryParse(parts[0], out year);
                    int.TryParse(parts[1], out monthNum);
                }
            }

            var (items, total, monthTotal) =
                await _repo.GetPagedForMonthAsync(shopId, year, monthNum, page, pageSize);

            return Ok(new PagedResponse<Expense>
            {
                Items = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize,
                MonthTotal = monthTotal
            });
        }

        [HttpPost("{shopId}")]
        [Authorize]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateExpenseDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var expense = new Expense
            {
                ShopId = shopId,
                CategoryId = dto.CategoryId,
                ExpenseDate = dto.ExpenseDate,
                Amount = dto.Amount,
                Description = dto.Description,
                PaymentMode = dto.PaymentMode,
                Reference = dto.Reference,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(expense);
            return Ok(ApiResponse<Expense>.Ok(expense, "Expense recorded"));
        }

        [HttpGet("~/api/expense-categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSystemCategories()
        {
            var cats = await _repo.GetCategoriesAsync(null);
            return Ok(ApiResponse<IEnumerable<ExpenseCategory>>.Ok(cats));
        }

        [HttpGet("~/api/expense-categories/{shopId}")]
        [Authorize]
        public async Task<IActionResult> GetShopCategories(int shopId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var cats = await _repo.GetCategoriesAsync(shopId);
            return Ok(ApiResponse<IEnumerable<ExpenseCategory>>.Ok(cats));
        }
    }
}