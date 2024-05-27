using BlogManagement.DBContext;
using BlogManagement.Models.Entity;
using BlogManagement.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Controllers
{
    [Authorize(Policy = "RequireAdminRole")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDBContext _context;

        public CategoryController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Where(c => !c.IsDeleted).Select(c => new CategoryVM
            {
                Id = c.Id,
                Name = c.Name,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate
            }).ToListAsync();

            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryVM categoryVM)
        {
            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    Name = categoryVM.Name,
                    CreatedBy = User.Identity.Name,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["message"] = "Category created successfully.";
                TempData["status"] = "success";
                return RedirectToAction(nameof(Index));
            }

            return View(categoryVM);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null || category.IsDeleted)
            {
                return NotFound();
            }

            var categoryVM = new CategoryVM
            {
                Id = category.Id,
                Name = category.Name,
                CreatedDate = category.CreatedDate,
                UpdatedDate = category.UpdatedDate
            };

            return View(categoryVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryVM categoryVM)
        {
            if (id != categoryVM.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var category = await _context.Categories.FindAsync(id);
                    if (category == null || category.IsDeleted)
                    {
                        return NotFound();
                    }

                    category.Name = categoryVM.Name;
                    category.UpdatedBy = User.Identity.Name;
                    category.UpdatedDate = DateTime.UtcNow;

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["message"] = "Category updated successfully.";
                    TempData["status"] = "success";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(id))
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

            return View(categoryVM);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null || category.IsDeleted)
            {
                return NotFound();
            }

            category.IsDeleted = true;
            category.UpdatedBy = User.Identity.Name;
            category.UpdatedDate = DateTime.UtcNow;

            _context.Update(category);
            await _context.SaveChangesAsync();

            TempData["message"] = "Category deleted successfully.";
            TempData["status"] = "success";
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
