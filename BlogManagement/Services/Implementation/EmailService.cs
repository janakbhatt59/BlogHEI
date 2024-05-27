using BlogManagement.Models;
using System.Net.Mail;
using System.Net;
using BlogManagement.Services.Interface;
using MongoDB.Driver;
using BlogManagement.DBContext;

namespace BlogManagement.Services.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly SmtpClient _smtpClient;
        private readonly SMTPConfig m;
        private readonly ILogger<EmailService> _logger;
        private readonly MongoDbContext _mongoDbContext;
        public EmailService(SmtpClient smtpClient, SMTPConfig m, MongoDbContext mongoDbContext, ILogger<EmailService> logger)
        {
            _smtpClient = smtpClient;
            this.m = m;
            _mongoDbContext = mongoDbContext;
            _logger = logger;
        }

        public async Task<bool> SendMailAsync(string toAddress, string subject, string msg, string user)
        {
            bool success = false;

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(m.SmtpFromAddress, m.SmtpDisplayName);
                    mail.Subject = subject;
                    mail.Body = msg;
                    mail.IsBodyHtml = true;

                    foreach (string to in toAddress.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)))
                    {
                        mail.To.Add(new MailAddress(to));
                    }

                    await _smtpClient.SendMailAsync(mail);
                    success = true;
                    var logEntry = new LogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Level = "Information",
                        Message = $"Email sent to {toAddress} with subject {subject}",
                        User = user,
                        Source = "EmailService",
                        AdditionalInfo = msg
                    };

                    await _mongoDbContext.LogEntries.InsertOneAsync(logEntry);
                    _logger.LogInformation("Email sent successfully.");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");
                return success;
            }
            return success;
        }

        public async Task<bool> SendEmailToMultipleUsersAsync(IEnumerable<string> emails, string subject, string msg, string user)
        {
            bool success = false;

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(m.SmtpFromAddress, m.SmtpDisplayName);
                    mail.Subject = subject;
                    mail.Body = msg;
                    mail.IsBodyHtml = true;

                    foreach (string to in emails)
                    {
                        mail.To.Add(new MailAddress(to));
                    }

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    await _smtpClient.SendMailAsync(mail);
                    success = true;
                    foreach (string to in emails)
                    {
                        var logEntry = new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "Information",
                            Message = $"Email sent to {to} with subject {subject}",
                            User = user,
                            Source = "EmailService",
                            AdditionalInfo = msg
                        };
                        await _mongoDbContext.LogEntries.InsertOneAsync(logEntry);

                    }
                    _logger.LogInformation("Email sent successfully.");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");

                return success;
            }

            return success;
        }
    }
}
