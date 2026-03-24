// ================================================
//  AgroBillling.API / Controllers / ExpensesController.cs
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "SHOP")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseRepository _repo;

        public ExpensesController(IExpenseRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("api/expenses/{shopId}")]
        public async Task<IActionResult> GetAll(int shopId,
            [FromQuery] string? month,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            int year  = DateTime.Now.Year;
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
                Items      = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize   = pageSize,
                MonthTotal = monthTotal
            });
        }

        [HttpPost("api/expenses/{shopId}")]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateExpenseDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var expense = new Expense
            {
                ShopId      = shopId,
                CategoryId  = dto.CategoryId,
                ExpenseDate = dto.ExpenseDate,
                Amount      = dto.Amount,
                Description = dto.Description,
                PaymentMode = dto.PaymentMode,
                Reference   = dto.Reference,
                CreatedAt   = DateTime.Now
            };

            await _repo.AddAsync(expense);
            return Ok(ApiResponse<Expense>.Ok(expense, "Expense recorded"));
        }

        [HttpGet("api/expense-categories")]
        public async Task<IActionResult> GetSystemCategories()
        {
            var cats = await _repo.GetCategoriesAsync(null);
            return Ok(ApiResponse<IEnumerable<ExpenseCategory>>.Ok(cats));
        }

        [HttpGet("api/expense-categories/{shopId}")]
        public async Task<IActionResult> GetShopCategories(int shopId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var cats = await _repo.GetCategoriesAsync(shopId);
            return Ok(ApiResponse<IEnumerable<ExpenseCategory>>.Ok(cats));
        }

        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            return int.TryParse(claim, out var id) && id == shopId;
        }
    }
}
