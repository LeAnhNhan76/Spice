using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _db.Category.ToListAsync());
        }
    }
}