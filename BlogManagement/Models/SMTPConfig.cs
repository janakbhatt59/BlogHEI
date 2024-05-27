namespace BlogManagement.Models
{
    public class SMTPConfig
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool SmtpSsl { get; set; }
        public string SmtpFromAddress { get; set; }
        public string SmtpDisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}
