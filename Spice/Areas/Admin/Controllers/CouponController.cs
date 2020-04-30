using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    public class CouponController : Controller
    {
        #region Properties

        private ApplicationDbContext _db;

        #endregion

        #region Constructors
        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }
        #endregion

        #region Methods

        #region List

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var coupon = await _db.Coupon.ToListAsync();
            return View(coupon);
        }

        #endregion

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
                if(files.Count > 0)
                {
                    byte[] picture = null;
                    using(var fileStream = files[0].OpenReadStream())
                    {
                        using(var memoryStream = new MemoryStream())
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

        #endregion

        #region Details
        #endregion

        #region Delete
        #endregion

        #endregion
    }
}