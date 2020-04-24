using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    public class CategoryController : Controller
    {
        #region Properties

        private readonly ApplicationDbContext _db;

        #endregion Properties

        #region Constructor

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion Constructor

        #region Methods

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _db.Category.ToListAsync());
        }
        
        [HttpGet]
        public IActionResult Create()
        {
            return  View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Add(category);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var category = await _db.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Update(category);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var category = await _db.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var category = await _db.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName(Constant.Action_Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _db.Category.FindAsync(id);
            if(category == null)
            {
                return NotFound();
            }
            _db.Category.Remove(category);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        #endregion Methods
    }
}