using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area(Constant.Area_Customer)]
    public class OrderController : Controller
    {
        #region Variables and Properties

        private readonly ApplicationDbContext _db;

        #endregion Variables and Properties

        #region Constructors

        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion Constructors

        #region Methods

        #region Confirm

        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailViewModel orderDetailVM = new OrderDetailViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id && o.UserId == claim.Value),
                OrderDetails = await _db.OrderDetail.Where(od => od.OrderId == id).ToListAsync()
            };
            return View(orderDetailVM);
        }

        #endregion Confirm

        #region Order History

        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            List<OrderDetailViewModel> orderList = new List<OrderDetailViewModel>();
            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(o => o.UserId == claim.Value).ToListAsync();

            foreach (var item in orderHeaderList)
            {
                var order = new OrderDetailViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetail.Where(od => od.OrderId == item.Id).ToListAsync()
                };
                orderList.Add(order);
            }

            return View(orderList);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            OrderDetailViewModel orderDetailVM = new OrderDetailViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id),
                OrderDetails = await _db.OrderDetail.Where(od => od.OrderId == id).ToListAsync()
            };
            orderDetailVM.OrderHeader.ApplicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == orderDetailVM.OrderHeader.UserId);
            return PartialView(Constant.PartialView_IndividualOrderDetailsPartial, orderDetailVM);
        }

        #endregion Order History

        public IActionResult Index()
        {
            return View();
        }

        #endregion Methods
    }
}