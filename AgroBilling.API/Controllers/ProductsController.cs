// ================================================
//  AgroBillling.API / Controllers / ProductsController.cs
//  ✅ COMPLETE FIXED VERSION - Reads role claim correctly
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _repo;
        private readonly AgroBillingDbContext _context;

        public ProductsController(IProductRepository repo, AgroBillingDbContext context)
        {
            _repo = repo;
            _context = context;
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
            // Try all possible role claim types
            var role = User.FindFirst(ClaimTypes.Role)?.Value     // This is the proper one
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

        // ─── PRODUCTS ────────────────────────────────────────────

        [HttpGet("products/{shopId}")]
        [Authorize]
        public async Task<IActionResult> GetAll(int shopId,
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var (items, total) = await _repo.GetPagedByShopIdAsync(shopId, search, categoryId, page, pageSize);
            return Ok(new PagedResponse<Product>
            {
                Items = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        [HttpGet("products/detail/{productId}")]
        [Authorize]
        public async Task<IActionResult> GetById(int productId)
        {
            var product = await _repo.GetByIdAsync(productId);
            if (product == null) return NotFound(ApiResponse<string>.Fail("Product not found"));

            if (!IsShopAuthorized(product.ShopId)) return Forbid();

            return Ok(ApiResponse<Product>.Ok(product));
        }

        [HttpGet("products/full/{productId}")]
        [Authorize]
        public async Task<IActionResult> GetFullDetail(int productId)
        {
            var product = await _repo.GetDetailAsync(productId);
            if (product == null) return NotFound(ApiResponse<string>.Fail("Product not found"));

            if (!IsShopAuthorized(product.ShopId)) return Forbid();

            return Ok(ApiResponse<Product>.Ok(product));
        }

        [HttpGet("products/{shopId}/low-stock")]
        [Authorize]
        public async Task<IActionResult> GetLowStock(int shopId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var products = await _repo.GetLowStockAsync(shopId);
            return Ok(ApiResponse<IEnumerable<Product>>.Ok(products));
        }

        [HttpPost("products/{shopId}")]
        [Authorize]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateProductDto dto)
        {
            Console.WriteLine($"=== CREATE PRODUCT CALLED ===");
            Console.WriteLine($"ShopId: {shopId}");
            Console.WriteLine($"User authenticated: {User.Identity?.IsAuthenticated}");

            // Get role using the helper method
            var role = GetUserRole();
            Console.WriteLine($"Role found: '{role}'");

            // Also log all claims for debugging
            Console.WriteLine("All claims:");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"  {claim.Type} = {claim.Value}");
            }

            if (role != "SHOP" && role != "ADMIN")
            {
                Console.WriteLine($"Access denied. Role: '{role}'");
                return Forbid();
            }

            if (!IsShopAuthorized(shopId))
            {
                Console.WriteLine($"Shop authorization failed for shop {shopId}");
                return Forbid();
            }

            if (dto == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Product data is required"));
            }

            var product = new Product
            {
                ShopId = shopId,
                CategoryId = dto.CategoryId,
                SupplierId = dto.SupplierId,
                ProductName = dto.ProductName,
                CompanyName = dto.CompanyName,
                Hsncode = dto.HsnCode,
                UnitId = dto.UnitId,
                PurchasePrice = dto.PurchasePrice,
                SellingPrice = dto.SellingPrice,
                Gstpercent = dto.GstPercent,
                UseShopGst = dto.UseShopGst,
                CurrentStock = dto.CurrentStock,
                MinStockAlert = dto.MinStockAlert,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(product);
            _repo.InvalidateProductCache(shopId);

            Console.WriteLine($"Product created successfully with ID: {product.ProductId}");
            return Ok(ApiResponse<Product>.Ok(product, "Product added successfully"));
        }

        [HttpPut("products/{productId}")]
        [Authorize]
        public async Task<IActionResult> Update(int productId, [FromBody] CreateProductDto dto)
        {
            var product = await _repo.GetByIdAsync(productId);
            if (product == null) return NotFound(ApiResponse<string>.Fail("Product not found"));

            if (!IsShopAuthorized(product.ShopId)) return Forbid();

            product.ProductName = dto.ProductName;
            product.CompanyName = dto.CompanyName;
            product.CategoryId = dto.CategoryId;
            product.SupplierId = dto.SupplierId;
            product.PurchasePrice = dto.PurchasePrice;
            product.SellingPrice = dto.SellingPrice;
            product.Gstpercent = dto.GstPercent;
            product.UseShopGst = dto.UseShopGst;
            product.MinStockAlert = dto.MinStockAlert;

            await _repo.UpdateAsync(product);
            _repo.InvalidateProductCache(product.ShopId);
            return Ok(ApiResponse<Product>.Ok(product));
        }

        // ─── CATEGORIES ──────────────────────────────────────────

        [HttpGet("categories/{shopId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories(int shopId)
        {
            var cats = await _context.ProductCategories
                .AsNoTracking()
                .Where(c => c.ShopId == shopId && c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return Ok(ApiResponse<List<ProductCategory>>.Ok(cats));
        }

        [HttpPost("categories/{shopId}")]
        [Authorize]
        public async Task<IActionResult> CreateCategory(int shopId, [FromBody] string categoryName)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var cat = new ProductCategory
            {
                ShopId = shopId,
                CategoryName = categoryName,
                IsActive = true
            };

            await _context.ProductCategories.AddAsync(cat);
            await _context.SaveChangesAsync();
            _repo.InvalidateProductCache(shopId);
            return Ok(ApiResponse<ProductCategory>.Ok(cat));
        }

        // ─── UNITS ───────────────────────────────────────────────

        [HttpGet("units")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUnits()
        {
            var units = await _context.Units
                .AsNoTracking()
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            return Ok(ApiResponse<List<Unit>>.Ok(units));
        }
    }
}