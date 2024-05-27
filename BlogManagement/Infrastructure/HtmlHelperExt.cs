using BlogManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BlogManagement.Infrastructure
{
    public static class HtmlHelperExt
    {

        public static string ProfilePictureImage(this IHttpContextAccessor httpContextAccessor)
        {
            var profilePictureBase64 = httpContextAccessor.HttpContext.Session.GetString("ProfilePictureBase64");

            if (!string.IsNullOrEmpty(profilePictureBase64))
            {
                return profilePictureBase64;
            }
            return "";
        }

    }
}
