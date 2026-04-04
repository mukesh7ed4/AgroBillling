// ================================================
//  AgroBilling.API / Services / EmailService.cs
//  NEW FILE — Services folder mein banao
// ================================================

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AgroBilling.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOtpAsync(string toEmail, string shopName, string otp)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _config["Email:SenderName"] ?? "AgroBilling",
                    _config["Email:SenderEmail"]
                ));
                message.To.Add(new MailboxAddress(shopName, toEmail));
                message.Subject = $"AgroBilling — Your OTP: {otp}";

                message.Body = new TextPart("html")
                {
                    Text = $"""
                    <!DOCTYPE html>
                    <html>
                    <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:20px">
                      <div style="max-width:480px;margin:auto;background:#fff;border-radius:12px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,0.1)">
                        <h2 style="color:#2E7D32;margin-top:0">🌿 AgroBilling</h2>
                        <p style="color:#333">Namaste <b>{shopName}</b>,</p>
                        <p style="color:#555">Aapka Email Verification OTP:</p>
                        <div style="font-size:40px;font-weight:bold;letter-spacing:10px;
                                    color:#2E7D32;text-align:center;padding:24px;
                                    background:#f0f9f0;border:2px dashed #4CAF50;
                                    border-radius:12px;margin:20px 0">
                          {otp}
                        </div>
                        <p style="color:#888;font-size:14px">
                          ⏰ Yeh OTP <b>10 minutes</b> mein expire ho jaayega.
                        </p>
                        <hr style="border:none;border-top:1px solid #eee;margin:20px 0" />
                        <p style="color:#aaa;font-size:12px">
                          Agar aapne AgroBilling pe signup nahi kiya, toh is email ko ignore karein.
                        </p>
                      </div>
                    </body>
                    </html>
                    """
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _config["Email:SmtpHost"] ?? "smtp.gmail.com",
                    int.Parse(_config["Email:SmtpPort"] ?? "587"),
                    SecureSocketOptions.StartTls
                );
                await smtp.AuthenticateAsync(
                    _config["Email:SenderEmail"],
                    _config["Email:AppPassword"]
                );
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("OTP email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
                throw;
            }
        }
    }
}