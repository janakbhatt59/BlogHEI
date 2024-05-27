using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;
using BlogManagement.DBContext;
using BlogManagement.Hubs;
using BlogManagement.Models.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using BlogManagement.Models.ViewModel;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Identity;
using BlogManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlogManagement.Services.Interface;

[Authorize]
public class BlogController : Controller
{
    private readonly ApplicationDBContext _context;
    private readonly IHubContext<BlogHub> _hubContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;


    public BlogController(ApplicationDBContext context, IHubContext<BlogHub> hubContext, UserManager<ApplicationUser> userManager, IEmailService emailService)
    {
        _context = context;
        _hubContext = hubContext;
        _userManager = userManager;
        _emailService = emailService;
    }

    //public async Task<IActionResult> Index()
    //{
    //    var blogPosts = await _context.Blog
    //        .Where(bp => !bp.IsDeleted && !bp.IsDraft)
    //        .Include(bp => bp.Category)
    //        .Include(bp => bp.BlogPostTags)
    //            .ThenInclude(bpt => bpt.Tag)
    //        .ToListAsync();
    //    var data = blogPosts.Select(each => ToBlogVM(each));
    //    var user = await _userManager.GetUserAsync(User);
    //    var userRole = await _userManager.GetRolesAsync(user);
    //    ViewBag.UserRole = userRole.FirstOrDefault();
    //    return View(data);
    //}
    public async Task<IActionResult> Index(int pageNumber = 1, int itemsPerPage = 10, bool showOnlyDraft = false, int? categoryId = null, string title = null)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            IQueryable<Blog> blogQuery = _context.Blog
                .Where(bp => !bp.IsDeleted && (showOnlyDraft ? bp.IsDraft : !bp.IsDraft))
                .Include(bp => bp.Category);

            if (!isAdmin || showOnlyDraft)
            {
                blogQuery = blogQuery.Where(bp => bp.CreatedBy == user.Id);
            }

            if (categoryId.HasValue && categoryId.Value != 0)
            {
                blogQuery = blogQuery.Where(bp => bp.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(title))
            {
                blogQuery = blogQuery.Where(bp => bp.Title.Contains(title));
            }

            int totalRecords = await blogQuery.CountAsync();

            IQueryable<Blog> blogQueryDraft = _context.Blog
                .Where(bp => !bp.IsDeleted && bp.IsDraft && bp.CreatedBy == user.Id);
            int totalRecordsDraft = await blogQueryDraft.CountAsync();
            ViewBag.DraftCount = totalRecordsDraft;

            var userRole = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = userRole.FirstOrDefault();

            int skip = (pageNumber - 1) * itemsPerPage;

            var blogPosts = await blogQuery
                .OrderBy(bp => bp.CreatedDate)
                .Skip(skip)
                .Take(itemsPerPage)
                .ToListAsync();

            var data = blogPosts.Select(each => ToBlogVM(each));

            var pager = new Pager
            {
                PageNo = pageNumber,
                ItemsPerPage = itemsPerPage,
                TotalRecords = totalRecords
            };
            ViewData["Categories"] = new SelectList(_context.Categories.Where(c => !c.IsDeleted), "Id", "Name");

            return View(new PagedDataItem<BlogVM> { Data = data, Pager = pager });
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
    public async Task<IActionResult> Drafts()
    {
        var blogPosts = await _context.Blog
            .Where(bp => !bp.IsDeleted && bp.IsDraft)
            .Include(bp => bp.Category)
            .ToListAsync();
        var user = await _userManager.GetUserAsync(User);
        var userRole = await _userManager.GetRolesAsync(user);
        ViewBag.UserRole = userRole.FirstOrDefault();
        return View(blogPosts);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(BlogVM blogPost)
    {
        if (ModelState.IsValid)
        {
            Blog data = new Blog()
            {
                Title = blogPost.Title,
                Content = blogPost.Content,
                CategoryId = blogPost.CategoryId
            };
            data.IsDraft = true;

            if (data.Id == 0)
            {
                data.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                data.CreatedDate = DateTime.UtcNow;
                _context.Add(data);
            }
            else
            {
                data.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                data.UpdatedDate = DateTime.UtcNow;
                _context.Update(data);
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Draft saved successfully" });
        }

        return BadRequest(new { message = "Failed to save draft" });
    }

    public async Task<IActionResult> Create()
    {
        ViewData["Categories"] = new SelectList(_context.Categories.Where(c => !c.IsDeleted), "Id", "Name");
        var model = new BlogVM();
        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Create(BlogVM blogPost, IFormFile blogPhotoFile)
    {
        if (ModelState.IsValid)
        {
            Blog data = new Blog()
            {
                Title = blogPost.Title,
                Content = blogPost.Content,
                CategoryId = blogPost.CategoryId
            };
            data.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            data.CreatedDate = DateTime.UtcNow;
            data.PublishedAt = DateTime.UtcNow;
            data.IsDraft = false;
            if (blogPhotoFile != null && blogPhotoFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await blogPhotoFile.CopyToAsync(memoryStream);
                    data.BlogPhoto = memoryStream.ToArray();
                }
            }
            _context.Add(data);
            await _context.SaveChangesAsync();
            var user = await _userManager.GetUserAsync(User);
            var userRole = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = userRole.FirstOrDefault();
            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", data.Id.ToString());
            var adminEmails = await _userManager.GetUsersInRoleAsync("Admin");
            var adminEmailList = adminEmails.Select(u => u.Email).ToList();

            await _emailService.SendEmailToMultipleUsersAsync(adminEmailList, "New Blog Created", $"New Blog with title: {blogPost.Title} is created by user: {user.UserName}", User.Identity.Name);
            TempData["Message"] = "Blog created successfully";
            TempData["Status"] = "success";
            return RedirectToAction(nameof(Index));

        }
        ViewData["Categories"] = new SelectList(_context.Categories.Where(c => !c.IsDeleted), "Id", "Name", blogPost.CategoryId);
        return View(blogPost);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var blogPost = await _context.Blog
            .FirstOrDefaultAsync(bp => bp.Id == id);
        if (blogPost == null)
        {
            return NotFound();
        }
        ViewData["Categories"] = new SelectList(_context.Categories.Where(c => !c.IsDeleted), "Id", "Name", blogPost.CategoryId);
        var model = new BlogVM
        {
            Id = blogPost.Id,
            Title = blogPost.Title,
            Content = blogPost.Content,
            BlogPhotoBase64 = blogPost.BlogPhoto != null ? Convert.ToBase64String(blogPost.BlogPhoto) : null,
            CategoryId = blogPost.CategoryId,
            PublishedAt = blogPost.PublishedAt
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BlogVM blogPost, IFormFile? blogPhotoFile)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var existingBlogPost = await _context.Blog
                    .FirstOrDefaultAsync(bp => bp.Id == id && !bp.IsDeleted);
                existingBlogPost.Title = blogPost.Title;
                existingBlogPost.Content = blogPost.Content;
                existingBlogPost.CategoryId = blogPost.CategoryId;
                existingBlogPost.UpdatedDate = DateTime.UtcNow;
                existingBlogPost.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                existingBlogPost.IsDraft = false;
                if (blogPhotoFile != null && blogPhotoFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await blogPhotoFile.CopyToAsync(memoryStream);
                        existingBlogPost.BlogPhoto = memoryStream.ToArray();
                    }
                }

                _context.Update(existingBlogPost);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveUpdate", blogPost.Content);
                TempData["Message"] = "Blog updated successfully";
                TempData["Status"] = "success";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BlogPostExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["Categories"] = new SelectList(_context.Categories.Where(c => !c.IsDeleted), "Id", "Name", blogPost.CategoryId);
        return View(blogPost);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var blogPost = await _context.Blog.FindAsync(id);
        if (blogPost != null)
        {
            blogPost.IsDeleted = true;
            _context.Blog.Update(blogPost);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", blogPost.Id.ToString());
            TempData["Message"] = "Blog deleted successfully";
            TempData["Status"] = "success";
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> ImageUpload(IFormFile upload)
    {
        if (upload != null && upload.Length > 0)
        {
            using (var memoryStream = new MemoryStream())
            {
                await upload.CopyToAsync(memoryStream);
                var base64String = Convert.ToBase64String(memoryStream.ToArray());
                var fileUrl = $"data:{upload.ContentType};base64,{base64String}";

                return Json(new { uploaded = 1, fileName = upload.FileName, url = fileUrl });
            }
        }

        return Json(new { uploaded = 0, error = new { message = "Upload failed" } });
    }
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var blogPost = await _context.Blog
            .Include(a=> a.Category)
            .Where(bp => !bp.IsDeleted && !bp.IsDraft && bp.Id == id)
            .FirstOrDefaultAsync();

        if (blogPost == null)
        {
            return NotFound();
        }

        var blogVM = new BlogVM
        {
            Id = blogPost.Id,
            Title = blogPost.Title,
            Content = blogPost.Content,
            CategoryId = blogPost.CategoryId,
            BlogPhotoBase64 = blogPost.BlogPhoto != null ? Convert.ToBase64String(blogPost.BlogPhoto) : null,
            PublishedAt = blogPost.PublishedAt,
            Category = blogPost.Category,
            CreatedDate = blogPost.CreatedDate
        };

        return View(blogVM);
    }

    private bool BlogPostExists(int id)
    {
        return _context.Blog.Any(e => e.Id == id);
    }
}
