using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
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
        public async Task<IActionResult> OrderHistory(int pageIndex = 1)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailViewModel>()
            };

            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(o => o.UserId == claim.Value).ToListAsync();

            foreach (var item in orderHeaderList)
            {
                var order = new OrderDetailViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetail.Where(od => od.OrderId == item.Id).ToListAsync()
                };
                orderListVM.Orders.Add(order);
            }

            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders
                .OrderByDescending(o => o.OrderHeader.Id)
                .Skip((pageIndex - 1) * Constant.pageSize)
                .Take(Constant.pageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo()
            {
                CurrentPage = pageIndex,
                ItemsPerPage = Constant.pageSize,
                TotalItem = count,
                urlParam = string.Concat(
                  "/", Constant.Area_Customer,
                  "/", Constant.Controller_Order,
                  "/", Constant.Action_OrderHistory,
                  "?", Constant.Parameter_PageIndex,
                  ":")
            };

            return View(orderListVM);
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

        [HttpGet]
        public IActionResult GetOrderStatus(int Id)
        {
            return PartialView(Constant.PartialView_OrderStatusPartial, _db.OrderHeader.Where(o => o.Id == Id).FirstOrDefault().Status);
        }

        #endregion Order History

        #region Manage Order

        [Authorize(Roles = Constant.KitchenUser + "," + Constant.ManagerUser)]
        public async Task<IActionResult> ManageOrder(int pageIndex = 1)
        {
            List<OrderDetailViewModel> orderDetailsVM = new List<OrderDetailViewModel>();

            List<OrderHeader> orderHeaderList = await _db.OrderHeader
                .Where(o => o.Status == Constant.Status_Submitted || o.Status == Constant.Status_InProcess)
                .OrderBy(o => o.PickupTime).ToListAsync();

            foreach (var item in orderHeaderList)
            {
                var order = new OrderDetailViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetail.Where(od => od.OrderId == item.Id).ToListAsync()
                };
                orderDetailsVM.Add(order);
            }

            return View(orderDetailsVM);
        }

        #endregion Manage Order

        public IActionResult Index()
        {
            return View();
        }

        #endregion Methods
    }
}