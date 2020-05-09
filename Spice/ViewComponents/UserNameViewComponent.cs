using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.ViewComponents
{
    public class UserNameViewComponent : ViewComponent
    {
        #region Variables and Properties
        private readonly ApplicationDbContext _db;
        #endregion
        #region Constructors
        public UserNameViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }
        #endregion
        #region Methods
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var userFromDb = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == claim.Value);
            return View(userFromDb);
        }
        #endregion

    }
}