using System.Text.Json;

namespace AgroBilling.API.Services
{
    public class EmailValidationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<EmailValidationService> _logger;

        public EmailValidationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<EmailValidationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> IsEmailDeliverableAsync(string email)
        {
            try
            {
                // Method 1: Check MX records (no API key needed)
                if (await HasValidMxRecordAsync(email))
                    return true;

                // Method 2: Use AbstractAPI (free tier available)
                // Sign up at: https://www.abstractapi.com/email-verification
                var apiKey = _config["EmailVerification:AbstractApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    return await CheckWithAbstractApiAsync(email, apiKey);
                }

                // Method 3: Basic validation (fallback)
                return IsValidEmailFormat(email) && !IsDisposableEmail(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email validation failed for {Email}", email);
                return true; // Allow on error
            }
        }

        private async Task<bool> HasValidMxRecordAsync(string email)
        {
            var domain = email.Split('@')[1];
            try
            {
                using var client = _httpClientFactory.CreateClient();
                // Use dns lookup service
                var response = await client.GetStringAsync($"https://dns.google/resolve?name={domain}&type=MX");
                return response.Contains("\"MX\"");
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckWithAbstractApiAsync(string email, string apiKey)
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync(
                $"https://emailvalidation.abstractapi.com/v1/?api_key={apiKey}&email={email}");

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var deliverability = root.GetProperty("deliverability").GetString();
            return deliverability == "DELIVERABLE";
        }

        private bool IsValidEmailFormat(string email)
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

        private bool IsDisposableEmail(string email)
        {
            var domain = email.Split('@')[1].ToLower();
            var disposableDomains = new[]
            {
                "tempmail.com", "10minutemail.com", "guerrillamail.com",
                "mailinator.com", "yopmail.com", "throwawaymail.com",
                "temp-mail.org", "fakeinbox.com", "dispostable.com"
            };
            return disposableDomains.Contains(domain);
        }
    }
}