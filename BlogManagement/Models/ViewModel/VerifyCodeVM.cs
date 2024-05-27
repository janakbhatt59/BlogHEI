namespace BlogManagement.Models.ViewModel
{
    public class VerifyCodeVM
    {
            public string Provider { get; set; }
            public string Code { get; set; }
            public string? ReturnUrl { get; set; }
            public bool RememberMe { get; set; }
            public bool RememberBrowser { get; set; }
    }
}
