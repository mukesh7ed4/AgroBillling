// ================================================
//  AgroBilling.DAL / Repositories / ProductRepository.cs
// ================================================

using System.Collections.Concurrent;
using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AgroBillling.DAL.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, byte> CacheKeyIndex = new();

        private static readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
        };

        public ProductRepository(AgroBillingDbContext context, IMemoryCache cache) : base(context)
        {
            _cache = cache;
        }

        private static string Key(string kind, int shopId, params object[] parts) =>
            $"prd:{kind}:{shopId}:{string.Join(':', parts)}";

        public void InvalidateProductCache(int shopId)
        {
            foreach (var k in CacheKeyIndex.Keys.ToArray())
            {
                var parts = k.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3 || !int.TryParse(parts[2], out var sid) || sid != shopId)
                    continue;
                _cache.Remove(k);
                CacheKeyIndex.TryRemove(k, out _);
            }
        }

        private void SetCache<T>(string cacheKey, T value)
        {
            CacheKeyIndex.TryAdd(cacheKey, 0);
            _cache.Set(cacheKey, value, CacheOptions);
        }

        public async Task<IEnumerable<Product>> GetByShopIdAsync(int shopId, string? search = null, int? categoryId = null)
        {
            var cacheKey = Key("all", shopId, search ?? "", categoryId ?? -1);
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Product>? cached) && cached != null)
                return cached;

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .Include(p => p.Supplier)
                .Where(p => p.ShopId == shopId && p.IsActive == true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(search));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var list = await query.OrderBy(p => p.ProductName).ToListAsync();
            SetCache(cacheKey, list);
            return list;
        }

        public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedByShopIdAsync(
            int shopId, string? search, int? categoryId, int page, int pageSize)
        {
            var cacheKey = Key("paged", shopId, search ?? "", categoryId ?? -1, page, pageSize);
            if (_cache.TryGetValue(cacheKey, out (IReadOnlyList<Product> Items, int TotalCount) hit))
                return hit;

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .Include(p => p.Supplier)
                .Where(p => p.ShopId == shopId && p.IsActive == true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(search));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = ((IReadOnlyList<Product>)items, total);
            SetCache(cacheKey, result);
            return result;
        }

        public async Task<IEnumerable<Product>> GetLowStockAsync(int shopId)
        {
            var cacheKey = Key("low", shopId);
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Product>? cached) && cached != null)
                return cached;

            var list = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .Include(p => p.Supplier)
                .Where(p => p.ShopId == shopId &&
                            p.IsActive == true &&
                            p.CurrentStock <= p.MinStockAlert)
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();

            SetCache(cacheKey, list);
            return list;
        }

        public async Task UpdateStockAsync(int productId, decimal quantityChange)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.CurrentStock += quantityChange;
                await _context.SaveChangesAsync();
                InvalidateProductCache(product.ShopId);
            }
        }
    }
}
