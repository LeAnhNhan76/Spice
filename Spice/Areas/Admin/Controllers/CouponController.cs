using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using System.IO;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    [Authorize(Roles = Constant.ManagerUser)]
    public class CouponController : Controller
    {
        #region Variables and Properties

        private ApplicationDbContext _db;

        #endregion Variables and Properties

        #region Constructors

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion Constructors

        #region Methods

        #region List

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var coupon = await _db.Coupon.ToListAsync();
            return View(coupon);
        }

        #endregion List

        #region Add & Edit

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                // get picture
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] picture = null;
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await fileStream.CopyToAsync(memoryStream);
                            picture = memoryStream.ToArray();
                        }
                    }
                    coupon.Picture = picture;
                }
                _db.Coupon.Add(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var coupon = await _db.Coupon.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                // get picture
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] picture = null;
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await fileStream.CopyToAsync(memoryStream);
                            picture = memoryStream.ToArray();
                        }
                    }
                    coupon.Picture = picture;
                }
                _db.Coupon.Update(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        #endregion Add & Edit

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var coupon = await _db.Coupon.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        #endregion Details

        #region Delete

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var coupon = await _db.Coupon.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        [HttpPost, ActionName(Constant.Action_Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _db.Coupon.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            _db.Coupon.Remove(coupon);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion Delete

        #endregion Methods
    }
}