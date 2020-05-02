using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Controllers
{
    [Area(Constant.Area_Customer)]
    public class HomeController : Controller
    {
        #region Properties

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        #endregion Properties

        #region Constructors

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        #endregion Constructors

        #region Methods

        #region List

        public async Task<IActionResult> Index()
        {
            IndexViewModel indexVM = new IndexViewModel()
            {
                MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync(),
                Category = await _db.Category.ToListAsync(),
                Coupon = await _db.Coupon.Where(x => x.IsActive == true).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(claim != null)
            {
                var countCart = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(Constant.Session_CartCount, countCart);
            }

            return View(indexVM);
        }

        #endregion List

        #region Details

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var menuItemFromDb = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).Where(m => m.Id == id).FirstOrDefaultAsync();
            if(menuItemFromDb == null)
            {
                return NotFound();
            }
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCart shoppingCart = new ShoppingCart()
            {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id,
                ApplicationUserId = claim.Value
            };
            return View(shoppingCart);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                shoppingCart.ApplicationUserId = claim.Value;

                ShoppingCart shoppingCartFromDb = await _db.ShoppingCart.Where(s => s.ApplicationUserId == shoppingCart.ApplicationUserId && s.MenuItemId == shoppingCart.MenuItemId).FirstOrDefaultAsync();
                if(shoppingCartFromDb == null)
                {
                   await  _db.ShoppingCart.AddAsync(shoppingCart);
                }
                else
                {
                    shoppingCartFromDb.Count += shoppingCart.Count;
                }
                await _db.SaveChangesAsync();

                var count = _db.ShoppingCart.Where(s => s.ApplicationUserId == shoppingCart.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(Constant.Session_CartCount, count);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var menuItemFromDb = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).Where(m => m.Id == shoppingCart.MenuItem.Id).FirstOrDefaultAsync();
                
                ShoppingCart cartObj = new ShoppingCart()
                {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };
                return View(cartObj);
            }
        }

        #endregion Details

        #endregion Methods
    }
}