using BlogManagement.DBContext;
using BlogManagement.Models;
using BlogManagement.Models.Entity;
using BlogManagement.Models.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BlogManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDBContext _context;
        public HomeController(ILogger<HomeController> logger, ApplicationDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string? titleSearch, int? categoryId, int pageNumber = 1, int itemsPerPage = 10)
        {
            try
            {
                IQueryable<Blog> blogQuery = _context.Blog
                    .Where(bp => !bp.IsDeleted && !bp.IsDraft)
                    .Include(bp => bp.Category)
                    .Include(bp => bp.User);

                // Apply title search filter if provided
                if (!string.IsNullOrEmpty(titleSearch))
                {
                    blogQuery = blogQuery.Where(bp => bp.Title.Contains(titleSearch));
                }

                // Apply category filter if provided
                if (categoryId.HasValue && categoryId > 0)
                {
                    blogQuery = blogQuery.Where(bp => bp.CategoryId == categoryId);
                }

                int totalRecords = await blogQuery.CountAsync();
                int skip = (pageNumber - 1) * itemsPerPage;

                var blogPosts = await blogQuery
                    .OrderBy(bp => bp.CreatedDate)
                    .Skip(skip)
                    .Take(itemsPerPage)
                    .ToListAsync();

                var data = blogPosts.Select(each => new BlogPostVM
                {
                    BlogVM = ToBlogVM(each),
                    PostedByName = each.User.FirstName + " " + each.User.LastName,
                    PostedByUserProfilePic = each.User.ProfilePicture != null ? Convert.ToBase64String(each.User.ProfilePicture) : null,
                });

                var pager = new Pager
                {
                    PageNo = pageNumber,
                    ItemsPerPage = itemsPerPage,
                    TotalRecords = totalRecords
                };
                ViewData["Categories"] = new SelectList(_context.Categories.Where(c => !c.IsDeleted), "Id", "Name");

                return View(new PagedDataItem<BlogPostVM> { Data = data, Pager = pager });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private BlogVM ToBlogVM(Blog blog)
        {
            var vm = new BlogVM
            {
                Id = blog.Id,
                Title = blog.Title,
                Content = blog.Content,
                PublishedAt = blog.PublishedAt,
                CategoryId = blog.CategoryId,
                BlogPhotoBase64 = blog.BlogPhoto != null ? Convert.ToBase64String(blog.BlogPhoto) : null,
                Category = blog.Category,
                CreatedDate = blog.CreatedDate
            };
            return vm;

        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
