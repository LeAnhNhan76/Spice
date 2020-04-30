using System;
using System.Collections.Generic;
using System.IO;
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

        [HttpPost, ActionName(Constant.Action_Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePOST()
        {
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());
            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }
            _db.MenuItem.Add(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            // Work on the image saving section
            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = _db.MenuItem.Find(MenuItemVM.MenuItem.Id);
            if(files.Count > 0)
            {
                // files have been uploaded
                var uploads = Path.Combine(webRootPath, Constant.Directory_Images);
                var extension = Path.GetExtension(files[0].FileName);

                using (var filesStream = new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id.ToString() + extension), FileMode.Create))
                {
                    files[0].CopyTo(filesStream);
                }
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extension;
            }
            else
            {
                // no file was uploaded, so use default
                var uploads = Path.Combine(webRootPath, @"images\", Constant.DefaultFoodImage);
                System.IO.File.Copy(uploads, webRootPath + @"\images\" + MenuItemVM.MenuItem.Id.ToString() + Constant.File_PNG);
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + Constant.File_PNG;
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).SingleOrDefaultAsync(x => x.Id == id);
            if(MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            MenuItemVM.SubCategory = await _db.SubCategory.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
            return View(MenuItemVM);
        }

        [HttpPost, ActionName(Constant.Action_Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());
            if (!ModelState.IsValid)
            {
                MenuItemVM.SubCategory = await _db.SubCategory.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
                return View(MenuItemVM);
            }

            // Work on the image saving section
            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = _db.MenuItem.Find(MenuItemVM.MenuItem.Id);
            if (files.Count > 0)
            {
                // new file have been uploaded
                var uploads = Path.Combine(webRootPath, Constant.Directory_Images);
                var extensionNew = Path.GetExtension(files[0].FileName);

                // delete the original file
                var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                // we will upload the new file
                using (var filesStream = new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id.ToString() + extensionNew), FileMode.Create))
                {
                    files[0].CopyTo(filesStream);
                }
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extensionNew;
            }

            menuItemFromDb.Name = MenuItemVM.MenuItem.Name;
            menuItemFromDb.Description = MenuItemVM.MenuItem.Description;
            menuItemFromDb.Price = MenuItemVM.MenuItem.Price;
            menuItemFromDb.Spicyness = MenuItemVM.MenuItem.Spicyness;
            menuItemFromDb.CategoryId = MenuItemVM.MenuItem.CategoryId;
            menuItemFromDb.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).SingleOrDefaultAsync(x => x.Id == id);
            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            MenuItemVM.SubCategory = await _db.SubCategory.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
            return View(MenuItemVM);
        }

        #endregion

        #region Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).SingleOrDefaultAsync(x => x.Id == id);
            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            MenuItemVM.SubCategory = await _db.SubCategory.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
            return View(MenuItemVM);
        }

        [HttpPost, ActionName(Constant.Action_Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var menuItemFromDb = await _db.MenuItem.FindAsync(id);
            if(menuItemFromDb == null)
            {
                return NotFound();
            }

            var webRootPath = _hostingEnvironment.WebRootPath;
            // Delete the image original
            var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));
            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound();
            }
            System.IO.File.Delete(imagePath);
            _db.MenuItem.Remove(menuItemFromDb);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #endregion
    }
}