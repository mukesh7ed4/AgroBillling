// ================================================
//  AgroBillling.API / Controllers / ReportsController.cs
// ================================================

using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "SHOP")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportRepository _repo;

        public ReportsController(IReportRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("{shopId}/monthly")]
        public async Task<IActionResult> GetMonthly(int shopId,
            [FromQuery] int year,
            [FromQuery] int month)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            if (year == 0)  year  = DateTime.Now.Year;
            if (month == 0) month = DateTime.Now.Month;

            var data = await _repo.GetMonthlyDashboardAsync(shopId, year, month);
            return Ok(ApiResponse<MonthlyDashboardDto>.Ok(data));
        }

        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            return int.TryParse(claim, out var id) && id == shopId;
        }
    }
}
