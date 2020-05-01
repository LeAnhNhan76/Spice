using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    public class UserController : Controller
    {
        #region Properties
        private readonly ApplicationDbContext _db;
        #endregion

        #region Constructors
        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }
        #endregion

        #region Methods

        #region List
        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var applicationUser = await _db.ApplicationUser.Where(x => x.Id != claim.Value).ToListAsync();
            return View(applicationUser);
        }
        #endregion

        #region Add & Edit
        #endregion

        #region Details
        #endregion

        #region Delete
        #endregion

        #endregion



    }
}