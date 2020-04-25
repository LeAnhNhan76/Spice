using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    public class SubCategoryController : Controller
    {
        #region Properties

        private ApplicationDbContext _db;

        #endregion Properties

        #region Constructors

        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion Constructors

        #region Methods

        public async Task<IActionResult> Index()
        {
            var subCategory = await _db.SubCategory.Include(x => x.Category).ToListAsync();
            return View(subCategory);
        }

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

        #endregion Methods
    }
}