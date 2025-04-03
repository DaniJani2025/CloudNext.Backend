using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;
using System;

namespace CloudNext.Services
{
    public class SMTPService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _appName;
        private readonly bool _enableEmail;

        public SMTPService(IConfiguration configuration)
        {
            _host = configuration["SmtpClient:Host"]!;
            _port = Convert.ToInt32(configuration["SmtpClient:Port"])!;
            _username = configuration["SmtpClient:Username"]!;
            _password = configuration["SmtpClient:Password"]!;
            _appName = configuration["SmtpClient:ApplicationName"]!;
            _enableEmail = false;
        }

        public async Task SendRegistrationMailAsync(string recipientEmail, string verificationUrl)
        {
            if (!_enableEmail) return;

            await Task.Run(async () =>
            {
                try
                {
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "verification_url_mail_templace.html");
                    string htmlTemplate = await File.ReadAllTextAsync(templatePath);

                    string emailBody = htmlTemplate
                        .Replace("{{verificationUrl}}", verificationUrl)
                        .Replace("{{year}}", DateTime.Now.Year.ToString());

                    using SmtpClient smtpClient = new(_host, _port)
                    {
                        EnableSsl = true,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(_username, _password)
                    };

                    using MailMessage mail = new()
                    {
                        From = new MailAddress(_username, _appName),
                        Subject = "Verify Your Email",
                        Body = emailBody,
                        IsBodyHtml = true
                    };
                    mail.To.Add(recipientEmail);

                    await smtpClient.SendMailAsync(mail);
                    Console.WriteLine($"Verification email sent to {recipientEmail}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send verification email: {ex.Message}");
                }
            });
        }

        public async Task SendOTPAsync(string recipientEmail, string otp)
        {
            if (!_enableEmail) return;

            await Task.Run(async () =>
            {
                try
                {
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "otp_mail_template.html");
                    string htmlTemplate = await File.ReadAllTextAsync(templatePath);

                    string emailBody = htmlTemplate
                        .Replace("{{otp}}", otp)
                        .Replace("{{year}}", DateTime.Now.Year.ToString());

                    using SmtpClient smtpClient = new(_host, _port)
                    {
                        EnableSsl = true,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(_username, _password)
                    };

                    using MailMessage mail = new()
                    {
                        From = new MailAddress(_username, _appName),
                        Subject = "Your OTP Code",
                        Body = emailBody,
                        IsBodyHtml = true
                    };
                    mail.To.Add(recipientEmail);

                    await smtpClient.SendMailAsync(mail);
                    Console.WriteLine("OTP sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send OTP: {ex.Message}");
                }
            });
        }

        public async Task SendWelcomeMessageAsync(string email)
        {
            if (!_enableEmail) return;

            await Task.Run(async () =>
            {
                try
                {
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "welcome_mail_template.html");
                    string htmlTemplate = await File.ReadAllTextAsync(templatePath);

                    using SmtpClient smtpClient = new(_host, _port)
                    {
                        EnableSsl = true,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(_username, _password)
                    };

                    using MailMessage mail = new()
                    {
                        From = new MailAddress(_username, _appName),
                        Subject = "Welcome to Our Service",
                        Body = htmlTemplate,
                        IsBodyHtml = true
                    };
                    mail.To.Add(email);

                    await smtpClient.SendMailAsync(mail);
                    Console.WriteLine("Welcome message sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send welcome message: {ex.Message}");
                }
            });
        }
    }
}
