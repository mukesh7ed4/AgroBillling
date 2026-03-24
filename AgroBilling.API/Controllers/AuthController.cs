using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AgroBillling.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _config;
        private readonly AgroBillingDbContext _context;

        public AuthController(
            IAuthRepository authRepo,
            IConfiguration config,
            AgroBillingDbContext context)
        {
            _authRepo = authRepo;
            _config = config;
            _context = context;
        }

        // ===========================
        // ✅ SIGNUP
        // ===========================
        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup([FromBody] SignupDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(ApiResponse<string>.Fail("All required fields must be provided"));

            // Email exists check
            var existing = await _context.Shops
                .FirstOrDefaultAsync(s => s.Email == dto.Email);

            if (existing != null)
                return BadRequest(ApiResponse<string>.Fail("Email already registered"));

            var passwordHash = HashPassword(dto.Password);

            var shop = new Shop
            {
                OwnerName = dto.OwnerName,
                ShopName = dto.ShopName,
                MobileNumber = dto.MobileNumber,
                Email = dto.Email,
                Address = dto.City,
                City = dto.City,
                State = dto.State ?? "Haryana",
                
                Gstpercent = 18,
                BillStartNumber = 1,
                CurrentBillSequence = 0,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedByAdminId = 1
            };

            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();

            // Trial plan (14 days)
            var trialPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.IsTrial == true);

            if (trialPlan != null)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);

                _context.ShopSubscriptions.Add(new ShopSubscription
                {
                    ShopId = shop.ShopId,
                    PlanId = trialPlan.PlanId,
                    StartDate = today,
                    EndDate = today.AddDays(14),
                    AmountPaid = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            // Admin notification
            _context.AdminNotifications.Add(new AdminNotification
            {
                ShopId = shop.ShopId,
                NotificationType = "NEW_SIGNUP",
                Message = $"New signup: {shop.ShopName} ({shop.OwnerName}) - {shop.MobileNumber}",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(
                new { shopId = shop.ShopId },
                "Account created successfully"
            ));
        }

        // ===========================
        // ✅ LOGIN (UPDATED)
        // ===========================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(ApiResponse<string>.Fail("Email and password are required."));

            var passwordHash = HashPassword(dto.Password);

            // ✅ 1. Admin check
            var admin = await _authRepo.ValidateAdminAsync(dto.Email, passwordHash);
            if (admin != null)
            {
                var token = GenerateToken(admin.AdminId.ToString(), "ADMIN", null, null);

                return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = Guid.NewGuid().ToString(),
                    Role = "ADMIN",
                    OwnerName = admin.FullName,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                }));
            }

            // ✅ 2. Shop check
            var shop = await _authRepo.ValidateShopAsync(dto.Email, passwordHash);
            if (shop != null)
            {
                var sub = await _context.ShopSubscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.ShopId == shop.ShopId && s.IsActive);

                var today = DateOnly.FromDateTime(DateTime.Now);
                var status = "EXPIRED";
                var expiry = "";

                if (sub != null)
                {
                    if (sub.EndDate >= today)
                        status = sub.Plan?.IsTrial == true ? "TRIAL" : "ACTIVE";

                    expiry = sub.EndDate.ToString("yyyy-MM-dd");
                }

                var token = GenerateToken(
                    shop.ShopId.ToString(),
                    "SHOP",
                    shop.ShopId,
                    shop.ShopName
                );

                return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = Guid.NewGuid().ToString(),
                    Role = "SHOP",
                    ShopId = shop.ShopId,
                    ShopName = shop.ShopName,
                    OwnerName = shop.OwnerName,
                    SubscriptionStatus = status,
                    SubscriptionExpiry = expiry,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                }));
            }

            return Unauthorized(ApiResponse<string>.Fail("Invalid email or password"));
        }

        // ===========================
        // ✅ JWT TOKEN
        // ===========================
        private string GenerateToken(string userId, string role, int? shopId, string? shopName)
        {
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("Jwt:Key missing");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(ClaimTypes.Role, role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (shopId.HasValue)
                claims.Add(new Claim("shopId", shopId.Value.ToString()));

            if (!string.IsNullOrEmpty(shopName))
                claims.Add(new Claim("shopName", shopName));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ===========================
        // ⚠️ HASH (keep as-is for now)
        // ===========================
        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}