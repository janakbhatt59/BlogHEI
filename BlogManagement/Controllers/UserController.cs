using BlogManagement.Models;
using BlogManagement.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var profilePictureBase64 = Convert.ToBase64String(user.ProfilePicture);
                ViewBag.ProfilePictureBase64 = profilePictureBase64;
            }
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserVM
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureBase64 = user.ProfilePicture != null ? Convert.ToBase64String(user.ProfilePicture) : null
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserVM model, IFormFile profilePictureFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (profilePictureFile != null && profilePictureFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await profilePictureFile.CopyToAsync(memoryStream);
                    user.ProfilePicture = memoryStream.ToArray();
                }
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["message"] = "User Details Updated Successfully.";
                TempData["status"] = "success";
                return RedirectToAction("Edit");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}