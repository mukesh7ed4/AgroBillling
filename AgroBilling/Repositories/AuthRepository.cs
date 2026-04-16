// ================================================
//  AgroBilling.DAL / Repositories / AuthRepository.cs
//  ✅ Debug logs removed — security fix
//  ✅ Null handling added for database null values
// ================================================

using AgroBilling.DAL.Context;
using AgroBilling.DAL.Models;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AgroBilling.DAL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AgroBillingDbContext _context;

        public AuthRepository(AgroBillingDbContext context)
        {
            _context = context;
        }

        public async Task<Shop?> ValidateShopAsync(string email, string plainPassword)
        {
            var hashedPassword = HashPassword(plainPassword);
            var shop = await _context.Shops
                .FirstOrDefaultAsync(s =>
                    s.Email != null &&
                    s.Email == email &&
                    s.PasswordHash == hashedPassword &&
                    s.IsActive == true);

            // Handle null values from database
            if (shop != null)
            {
                shop.AlternateMobile ??= string.Empty;
                shop.Email ??= string.Empty;
                shop.Gstnumber ??= string.Empty;
                shop.LogoPath ??= string.Empty;
            }

            return shop;
        }

        public async Task<AdminUser?> ValidateAdminAsync(string email, string plainPassword)
        {
            var hashedPassword = HashPassword(plainPassword);
            return await _context.AdminUsers
                .FirstOrDefaultAsync(a =>
                    a.Email == email &&
                    a.PasswordHash == hashedPassword &&
                    a.IsActive == true);
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}