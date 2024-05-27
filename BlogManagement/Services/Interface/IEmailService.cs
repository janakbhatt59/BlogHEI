namespace BlogManagement.Services.Interface
{
    public interface IEmailService
    {
        Task<bool> SendEmailToMultipleUsersAsync(IEnumerable<string> emails, string subject, string msg, string user);
        Task<bool> SendMailAsync(string toAddress, string subject, string msg, string user);
    }
}