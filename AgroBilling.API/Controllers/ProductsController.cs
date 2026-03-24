// ================================================
//  AgroBillling.API / Controllers / ProductsController.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "SHOP")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository  _repo;
        private readonly AgroBillingDbContext _context;

        public ProductsController(IProductRepository repo, AgroBillingDbContext context)
        {
            _repo    = repo;
            _context = context;
        }

        // ─── PRODUCTS ───
        [HttpGet("api/products/{shopId}")]
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
                Items      = items.ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize   = pageSize
            });
        }

        [HttpGet("api/products/detail/{productId}")]
        public async Task<IActionResult> GetById(int productId)
        {
            var product = await _repo.GetByIdAsync(productId);
            if (product == null) return NotFound(ApiResponse<string>.Fail("Product not found"));
            return Ok(ApiResponse<Product>.Ok(product));
        }

        [HttpGet("api/products/{shopId}/low-stock")]
        public async Task<IActionResult> GetLowStock(int shopId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var products = await _repo.GetLowStockAsync(shopId);
            return Ok(ApiResponse<IEnumerable<Product>>.Ok(products));
        }

        [HttpPost("api/products/{shopId}")]
        public async Task<IActionResult> Create(int shopId, [FromBody] CreateProductDto dto)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();

            var product = new Product
            {
                ShopId        = shopId,
                CategoryId    = dto.CategoryId,
                SupplierId    = dto.SupplierId,
                ProductName   = dto.ProductName,
                CompanyName   = dto.CompanyName,
                Hsncode       = dto.HsnCode,
                UnitId        = dto.UnitId,
                PurchasePrice = dto.PurchasePrice,
                SellingPrice  = dto.SellingPrice,
                Gstpercent    = dto.GstPercent,
                UseShopGst    = dto.UseShopGst,
                CurrentStock  = dto.CurrentStock,
                MinStockAlert = dto.MinStockAlert,
                IsActive      = true,
                CreatedAt     = DateTime.Now
            };

            await _repo.AddAsync(product);
            _repo.InvalidateProductCache(shopId);
            return Ok(ApiResponse<Product>.Ok(product, "Product added successfully"));
        }

        [HttpPut("api/products/{productId}")]
        public async Task<IActionResult> Update(int productId, [FromBody] CreateProductDto dto)
        {
            var product = await _repo.GetByIdAsync(productId);
            if (product == null) return NotFound(ApiResponse<string>.Fail("Product not found"));

            product.ProductName   = dto.ProductName;
            product.CompanyName   = dto.CompanyName;
            product.CategoryId    = dto.CategoryId;
            product.SupplierId    = dto.SupplierId;
            product.PurchasePrice = dto.PurchasePrice;
            product.SellingPrice  = dto.SellingPrice;
            product.Gstpercent    = dto.GstPercent;
            product.UseShopGst    = dto.UseShopGst;
            product.MinStockAlert = dto.MinStockAlert;

            await _repo.UpdateAsync(product);
            _repo.InvalidateProductCache(product.ShopId);
            return Ok(ApiResponse<Product>.Ok(product));
        }

        // ─── CATEGORIES ───
        [HttpGet("api/categories/{shopId}")]
        public async Task<IActionResult> GetCategories(int shopId)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var cats = await _context.ProductCategories
                .AsNoTracking()
                .Where(c => c.ShopId == shopId && c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            return Ok(ApiResponse<List<ProductCategory>>.Ok(cats));
        }

        [HttpPost("api/categories/{shopId}")]
        public async Task<IActionResult> CreateCategory(int shopId, [FromBody] string categoryName)
        {
            if (!IsShopAuthorized(shopId)) return Forbid();
            var cat = new ProductCategory
            {
                ShopId       = shopId,
                CategoryName = categoryName,
                IsActive     = true
            };
            await _context.ProductCategories.AddAsync(cat);
            await _context.SaveChangesAsync();
            _repo.InvalidateProductCache(shopId);
            return Ok(ApiResponse<ProductCategory>.Ok(cat));
        }

        // ─── UNITS ───
        [HttpGet("api/units")]
        public async Task<IActionResult> GetUnits()
        {
            var units = await _context.Units.AsNoTracking()
                .OrderBy(u => u.UnitName)
                .ToListAsync();
            return Ok(ApiResponse<List<Unit>>.Ok(units));
        }

        private bool IsShopAuthorized(int shopId)
        {
            var claim = User.FindFirst("shopId")?.Value;
            return int.TryParse(claim, out var id) && id == shopId;
        }
    }
}
