using BlogManagement.Models;
using BlogManagement.Models.ViewModel;
using BlogManagement.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlogManagement.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToLocal(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                TempData["message"] = "Model Validation Error.";
                TempData["status"] = "error";
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["message"] = "Invalid login attempt.";
                TempData["status"] = "error";
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.RequiresTwoFactor)
            {
                var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                if (string.IsNullOrWhiteSpace(code))
                {
                    TempData["message"] = "Failed to generate two-factor authentication code.";
                    TempData["status"] = "error";
                    ModelState.AddModelError(string.Empty, "Failed to generate two-factor authentication code.");
                    return View(model);
                }

                await _emailService.SendMailAsync(user.Email, "Security Code For Account Verification", $"Your security code for verification is: {code}", User.Identity.Name);
                TempData["message"] = "Verification Code is Send to the registerd email address.";
                TempData["status"] = "success";
                return RedirectToAction(nameof(VerifyCode), new { Provider = "Email", ReturnUrl = returnUrl, model.RememberMe });
            }

            if (result.Succeeded)
            {
                if (user.ProfilePicture != null)
                {
                    var profilePictureBase64 = Convert.ToBase64String(user.ProfilePicture);

                    HttpContext.Session.SetString("ProfilePictureBase64", profilePictureBase64);
                }
                _logger.LogInformation("User logged in.");
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                TempData["message"] = "User account locked out.";
                TempData["status"] = "error";
                _logger.LogWarning("User account locked out.");
                return View("Lockout");
            }

            TempData["message"] = "Invalid login attempt.";
            TempData["status"] = "error";
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await model.ProfilePicture.CopyToAsync(memoryStream);
                    user.ProfilePicture = memoryStream.ToArray();
                }
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                await _userManager.AddToRoleAsync(user, "Admin");
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);

                await _emailService.SendMailAsync(model.Email, "Verify Email Confirmation", $"Please confirm your account registration verification for blog management application by <a href='{callbackUrl}'>clicking here</a>.", User.Identity.Name);

                TempData["message"] = "User registered successfully. Please check your email to confirm your account.";
                TempData["status"] = "success";
                _logger.LogInformation("User created a new account with password.");
                return RedirectToAction("VerifyEmailNotification", "Account");
            }

            TempData["message"] = "Registration failed.";
            TempData["status"] = "error";
            AddErrors(result);
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["message"] = $"Unable to load user with ID '{userId}'.";
                TempData["status"] = "error";
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            var viewModel = new ConfirmEmailVM
            {
                StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email."
            };

            TempData["message"] = viewModel.StatusMessage;
            TempData["status"] = result.Succeeded ? "success" : "error";

            return View(viewModel);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out:" + User.Identity.Name);
            TempData["message"] = "User logged out successfully.";
            TempData["status"] = "success";
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException("Unable to load two-factor authentication user.");
            }

            return View(new VerifyCodeVM { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["message"] = "Invalid code.";
                TempData["status"] = "error";
                return View(model);
            }

            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                var currentUser = await _userManager.GetUserAsync(HttpContext.User);
                if (currentUser != null)
                {
                    if (currentUser.ProfilePicture != null)
                    {
                        var profilePictureBase64 = Convert.ToBase64String(currentUser.ProfilePicture);
                        HttpContext.Session.SetString("ProfilePictureBase64", profilePictureBase64);
                    }
                }
                return RedirectToLocal(model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                TempData["message"] = "User account locked out.";
                TempData["status"] = "error";
                return View("Lockout");
            }

            TempData["message"] = "Invalid code.";
            TempData["status"] = "error";
            ModelState.AddModelError(string.Empty, "Invalid code.");
            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl=null)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
        [AllowAnonymous]
        public IActionResult VerifyEmailNotification()
        {
            return View();
        }


        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
