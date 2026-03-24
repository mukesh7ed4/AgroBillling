// ================================================
//  AgroBilling.DAL / Repositories / AuthRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AgroBillingDbContext _context;

        public AuthRepository(AgroBillingDbContext context)
        {
            _context = context;
        }

        public async Task<Shop?> ValidateShopAsync(string email, string passwordHash)
        {
            return await _context.Shops
                .FirstOrDefaultAsync(s =>
                    s.Email == email &&
                    s.PasswordHash == passwordHash &&
                    s.IsActive == true);
        }

        public async Task<AdminUser?> ValidateAdminAsync(string email, string passwordHash)
        {
            return await _context.AdminUsers
                .FirstOrDefaultAsync(a =>
                    a.Email == email &&
                    a.PasswordHash == passwordHash &&
                    a.IsActive == true);
        }
    }
}
