using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    [Authorize(Roles = Constant.ManagerUser)]
    public class UserController : Controller
    {
        #region Variables and Properties

        private readonly ApplicationDbContext _db;

        #endregion Variables and Properties

        #region Constructors

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion Constructors

        #region Methods

        #region List

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var applicationUser = await _db.ApplicationUser.Where(u => u.Id != claim.Value).ToListAsync();
            return View(applicationUser);
        }

        #endregion List

        #region Block & Unblock

        public async Task<IActionResult> Lock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == id);
            if (applicationUser == null)
            {
                return NotFound();
            }
            applicationUser.LockoutEnd = DateTime.Now.AddYears(1000);
            _db.ApplicationUser.Update(applicationUser);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UnLock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == id);
            if (applicationUser == null)
            {
                return NotFound();
            }
            applicationUser.LockoutEnd = DateTime.Now;
            _db.ApplicationUser.Update(applicationUser);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion Block & Unblock

        #endregion Methods
    }
}