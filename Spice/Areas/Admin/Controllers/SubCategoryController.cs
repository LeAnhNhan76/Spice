using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    public class SubCategoryController : Controller
    {
        #region Properties

        private ApplicationDbContext _db;

        [TempData]
        public string StatusMessage { get; set; }

        #endregion Properties

        #region Constructors

        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion Constructors

        #region Methods

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var subCategory = await _db.SubCategory.Include(x => x.Category).ToListAsync();
            return View(subCategory);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(x => x.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doesSubCategroyExists = _db.SubCategory.Include( x => x.Category).Where(x => x.Name == model.SubCategory.Name && x.CategoryId == model.SubCategory.CategoryId);
                if (doesSubCategroyExists.Count() > 0)
                {
                    // Error
                    StatusMessage = "Error: Sub Category exists under " + doesSubCategroyExists.First().Category.Name + " category. Please use another name.";
                }
                else
                {
                    _db.SubCategory.Add(model.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            var modelVM = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(x => x.Name).Distinct().ToListAsync(),
                StatusMessage = StatusMessage
            };
            return View(modelVM);
        }

        [HttpPost, ActionName(Constant.Action_GetSubCategoryByCategory)]
        public async Task<IActionResult> GetSubCategoryByCategory(int id)
        {
            List<SubCategory> subCategory = new List<SubCategory>();
            subCategory = await _db.SubCategory.Where(x => x.CategoryId == id).ToListAsync();
            return Json(new SelectList(subCategory, "Id", "Name"));
        }

        #endregion Methods
    }
}