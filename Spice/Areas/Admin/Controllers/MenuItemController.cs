using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Areas.Admin.Controllers
{
    [Area(Constant.Area_Admin)]
    public class MenuItemController : Controller
    {
        #region Properties

        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        #endregion

        #region Constructors

        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel
            {
                MenuItem = new MenuItem(),
                Category = _db.Category.ToList()
            };
        }

        #endregion

        #region Methods

        #region List
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var menuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync();
            return View(menuItem);
        }

        #endregion

        #region Add & Edit
        [HttpGet]
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }

        #endregion

        #region Details
        #endregion

        #region Delete
        #endregion

        #endregion
    }
}