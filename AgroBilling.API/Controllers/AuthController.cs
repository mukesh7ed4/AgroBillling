using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using AgroBilling.API.Services;
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
        private readonly EmailService _emailService;
        private readonly EmailValidationService _emailValidationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthRepository authRepo,
            IConfiguration config,
            AgroBillingDbContext context,
            EmailService emailService,
            EmailValidationService emailValidationService,
            ILogger<AuthController> logger)
        {
            _authRepo = authRepo;
            _config = config;
            _context = context;
            _emailService = emailService;
            _emailValidationService = emailValidationService;
            _logger = logger;
        }

        [HttpGet("check-email")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmailExists([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Ok(new { exists = false });

            var exists = await _context.Shops
                .AnyAsync(s => s.Email != null && s.Email.ToLower() == email.ToLower() && s.IsEmailVerified == true);

            return Ok(new { exists, message = exists ? "Email already registered" : "Email available" });
        }

        [HttpGet("check-mobile")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckMobileExists([FromQuery] string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return Ok(new { exists = false });

            var exists = await _context.Shops
                .AnyAsync(s => s.MobileNumber == mobile);

            return Ok(new { exists, message = exists ? "Mobile number already registered" : "Mobile available" });
        }

        [HttpGet("validate-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Ok(new { isValid = false, message = "Email is required" });

            if (!IsValidEmail(email))
                return Ok(new { isValid = false, message = "Invalid email format" });

            var isDeliverable = await _emailValidationService.IsEmailDeliverableAsync(email);

            if (!isDeliverable)
                return Ok(new { isValid = false, message = "Email domain cannot receive emails. Please use a valid email address." });

            return Ok(new { isValid = true, message = "Email is valid" });
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup([FromBody] SignupDto dto)
        {
            Shop? shop = null;

            try
            {
                // ============================================
                // STEP 1: Validate all inputs
                // ============================================
                if (dto == null)
                    return BadRequest(ApiResponse<string>.Fail("Invalid request"));

                if (string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest(ApiResponse<string>.Fail("Email is required"));

                if (string.IsNullOrWhiteSpace(dto.Password))
                    return BadRequest(ApiResponse<string>.Fail("Password is required"));

                if (string.IsNullOrWhiteSpace(dto.ShopName))
                    return BadRequest(ApiResponse<string>.Fail("Shop name is required"));

                if (string.IsNullOrWhiteSpace(dto.OwnerName))
                    return BadRequest(ApiResponse<string>.Fail("Owner name is required"));

                if (string.IsNullOrWhiteSpace(dto.MobileNumber))
                    return BadRequest(ApiResponse<string>.Fail("Mobile number is required"));

                if (!IsValidMobileNumber(dto.MobileNumber))
                    return BadRequest(ApiResponse<string>.Fail("Please enter a valid 10-digit mobile number"));

                if (!IsValidEmail(dto.Email))
                    return BadRequest(ApiResponse<string>.Fail("Please enter a valid email address"));

                // ============================================
                // STEP 2: Check if mobile already registered
                // ============================================
                var existingMobile = await _context.Shops
                    .FirstOrDefaultAsync(s => s.MobileNumber == dto.MobileNumber);

                if (existingMobile != null)
                {
                    return BadRequest(ApiResponse<string>.Fail(
                        "This mobile number is already registered. Please use a different number or login."));
                }

                // ============================================
                // STEP 3: Check if email already verified
                // ============================================
                var existingVerifiedShop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Email != null &&
                        s.Email.ToLower() == dto.Email.ToLower() &&
                        s.IsEmailVerified == true);

                if (existingVerifiedShop != null)
                {
                    return BadRequest(ApiResponse<string>.Fail(
                        "This email is already registered. Please login instead."));
                }

                // ============================================
                // STEP 4: Handle unverified existing account
                // ============================================
                var existingUnverifiedShop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Email != null &&
                        s.Email.ToLower() == dto.Email.ToLower() &&
                        s.IsEmailVerified == false);

                if (existingUnverifiedShop != null)
                {
                    var newOtp = new Random().Next(100000, 999999).ToString();
                    existingUnverifiedShop.EmailOtp = newOtp;
                    existingUnverifiedShop.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _emailService.SendOtpAsync(
                            existingUnverifiedShop.Email,
                            existingUnverifiedShop.ShopName,
                            newOtp);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to resend OTP to {Email}", existingUnverifiedShop.Email);
                        return BadRequest(ApiResponse<string>.Fail(
                            "Could not send OTP. Please check your email address and try again."));
                    }

                    return Ok(ApiResponse<object>.Ok(new
                    {
                        email = existingUnverifiedShop.Email,
                        shopId = existingUnverifiedShop.ShopId,
                        requiresVerification = true,
                        message = "Account already exists but not verified. New OTP sent."
                    }, "New OTP sent to your email."));
                }

                // ============================================
                // STEP 5: Get admin ID
                // ============================================
                var adminId = await _context.AdminUsers
                    .Where(a => a.IsActive == true)
                    .Select(a => a.AdminId)
                    .FirstOrDefaultAsync();

                if (adminId == 0)
                    return StatusCode(500, ApiResponse<string>.Fail(
                        "System not configured. Please contact administrator."));

                // ============================================
                // STEP 6: Create shop object (NOT saved yet)
                // ============================================
                var passwordHash = HashPassword(dto.Password);
                var otp = new Random().Next(100000, 999999).ToString();
                var otpExpiry = DateTime.UtcNow.AddMinutes(10);

                shop = new Shop
                {
                    OwnerName = dto.OwnerName.Trim(),
                    ShopName = dto.ShopName.Trim(),
                    MobileNumber = dto.MobileNumber.Trim(),
                    AlternateMobile = null,
                    Email = dto.Email.Trim().ToLower(),
                    Address = dto.City ?? "N/A",
                    City = dto.City ?? "N/A",
                    State = dto.State ?? "Haryana",
                    PinCode = "000000",
                    Gstpercent = 18,
                    BillStartNumber = 1,
                    CurrentBillSequence = 0,
                    PasswordHash = passwordHash,
                    IsActive = false,
                    IsEmailVerified = false,
                    EmailOtp = otp,
                    OtpExpiresAt = otpExpiry,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAdminId = adminId
                };

                // ============================================
                // STEP 7: Send OTP email FIRST
                // ============================================
                try
                {
                    await _emailService.SendOtpAsync(shop.Email, shop.ShopName, otp);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "OTP email failed for {Email}", shop.Email);
                    return BadRequest(ApiResponse<string>.Fail(
                        "Could not send OTP to this email address. Please check your email and try again."));
                }

                // ============================================
                // STEP 8: ONLY AFTER email success, save to database
                // ============================================
                _context.Shops.Add(shop);
                await _context.SaveChangesAsync();

                // ============================================
                // STEP 9: Add trial subscription
                // ============================================
                var trialPlan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.IsTrial == true);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var trialDays = trialPlan?.DurationDays ?? 14;

                _context.ShopSubscriptions.Add(new ShopSubscription
                {
                    ShopId = shop.ShopId,
                    PlanId = trialPlan?.PlanId ?? 1,
                    StartDate = today,
                    EndDate = today.AddDays(trialDays),
                    AmountPaid = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(new
                {
                    email = shop.Email,
                    shopId = shop.ShopId,
                    requiresVerification = true,
                    message = "OTP sent to your email"
                }, "Registration successful! Please check your email for OTP."));
            }
            catch (Exception ex)
            {
                // ============================================
                // STEP 10: Rollback - delete shop if created
                // ============================================
                if (shop != null && shop.ShopId > 0)
                {
                    try
                    {
                        _context.Shops.Remove(shop);
                        await _context.SaveChangesAsync();
                        _logger.LogWarning("Rolled back shop creation for {Email} due to error", shop.Email);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Failed to rollback shop for {Email}", shop.Email);
                    }
                }

                _logger.LogError(ex, "Signup failed for email {Email}", dto?.Email);
                return StatusCode(500, ApiResponse<string>.Fail(
                    $"Registration failed: {ex.Message}"));
            }
        }

        private bool IsValidMobileNumber(string mobile)
        {
            return !string.IsNullOrWhiteSpace(mobile) &&
                   mobile.Length == 10 &&
                   mobile.All(char.IsDigit) &&
                   mobile[0] >= '6' && mobile[0] <= '9';
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Otp))
                return BadRequest(ApiResponse<string>.Fail("Email and OTP are required"));

            var shop = await _context.Shops
                .FirstOrDefaultAsync(s => s.Email == dto.Email);

            if (shop == null)
                return BadRequest(ApiResponse<string>.Fail("Shop not found"));

            if (shop.IsEmailVerified)
                return BadRequest(ApiResponse<string>.Fail("Email already verified. Please login."));

            if (shop.EmailOtp != dto.Otp)
                return BadRequest(ApiResponse<string>.Fail("Invalid OTP. Please check your email."));

            if (shop.OtpExpiresAt < DateTime.UtcNow)
            {
                // OTP expired - delete this unverified shop
                _context.Shops.Remove(shop);
                await _context.SaveChangesAsync();
                return BadRequest(ApiResponse<string>.Fail("OTP expired. Please signup again."));
            }

            shop.IsEmailVerified = true;
            shop.IsActive = true;
            shop.EmailOtp = null;
            shop.OtpExpiresAt = null;
            await _context.SaveChangesAsync();

            _context.AdminNotifications.Add(new AdminNotification
            {
                ShopId = shop.ShopId,
                NotificationType = "NEW_SIGNUP",
                Message = $"New verified signup: {shop.ShopName} ({shop.OwnerName}) - {shop.MobileNumber}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var sub = await _context.ShopSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.ShopId == shop.ShopId && s.IsActive);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var status = sub != null && sub.EndDate >= today
                ? (sub.Plan?.IsTrial == true ? "TRIAL" : "ACTIVE")
                : "EXPIRED";
            var expiry = sub?.EndDate.ToString("yyyy-MM-dd") ?? "";

            var token = GenerateToken(shop.ShopId.ToString(), "SHOP", shop.ShopId, shop.ShopName);

            return Ok(ApiResponse<object>.Ok(new
            {
                token,
                role = "SHOP",
                shopId = shop.ShopId,
                shopName = shop.ShopName,
                ownerName = shop.OwnerName,
                subscriptionStatus = status,
                subscriptionExpiry = expiry
            }, "Email verified! Welcome to AgroBilling."));
        }

        [HttpPost("resend-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(ApiResponse<string>.Fail("Email required"));

            var shop = await _context.Shops
                .FirstOrDefaultAsync(s => s.Email == dto.Email && s.IsEmailVerified == false);

            if (shop == null)
                return BadRequest(ApiResponse<string>.Fail("Email not found or already verified"));

            var otp = new Random().Next(100000, 999999).ToString();
            shop.EmailOtp = otp;
            shop.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpAsync(shop.Email, shop.ShopName, otp);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to resend OTP to {Email}", shop.Email);
                return BadRequest(ApiResponse<string>.Fail(
                    "Could not send OTP. Please check your email address."));
            }

            return Ok(ApiResponse<string>.Ok("ok", "OTP resent successfully"));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(ApiResponse<string>.Fail("Email and password are required."));

            var admin = await _authRepo.ValidateAdminAsync(dto.Email, dto.Password);
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

            var shop = await _authRepo.ValidateShopAsync(dto.Email, dto.Password);
            if (shop != null)
            {
                if (!shop.IsEmailVerified)
                    return Unauthorized(ApiResponse<string>.Fail(
                        "Email not verified. Please check your email for OTP."));

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

                var token = GenerateToken(shop.ShopId.ToString(), "SHOP", shop.ShopId, shop.ShopName);

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

        private string GenerateToken(string userId, string role, int? shopId, string? shopName)
        {
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("Jwt:Key missing");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role)
            };

            if (shopId.HasValue)
            {
                claims.Add(new Claim("shopId", shopId.Value.ToString()));
            }

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

        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}