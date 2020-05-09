using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Data
{
    public class DbInitializer : IDbInitializer
    {
        #region Variables and Properties
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        #endregion
        #region Constructors
        public DbInitializer(ApplicationDbContext db
            , UserManager<IdentityUser> userManager
            ,RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        #endregion
        #region Methods
        public async void Initialize()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch(Exception ex)
            {
                
            }

            if(_db.Roles.Any(r => r.Name == Constant.ManagerUser))
            {
                return;
            }
            _roleManager.CreateAsync(new IdentityRole { Name = Constant.ManagerUser }).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole { Name = Constant.KitchenUser }).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole { Name = Constant.FrontDeskUser }).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole { Name = Constant.CustomerEndUser }).GetAwaiter().GetResult();

            _userManager.CreateAsync(new ApplicationUser
            {
                UserName = Constant.DbInitialize_Manager_UserName,
                Email = Constant.DbInitialize_Manager_Email,
                Name = Constant.DbInitialize_Manager_Name,
                EmailConfirmed = true,
                PhoneNumber = Constant.DbInitialize_Manager_PhoneNumber
            }, Constant.DbInitialize_Manager_Password).GetAwaiter().GetResult();

            var user = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Email == Constant.DbInitialize_Manager_Email);

            await _userManager.AddToRoleAsync(user, Constant.ManagerUser);
        }
        #endregion

    }
}
