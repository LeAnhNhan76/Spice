using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Stripe;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area(Constant.Area_Customer)]
    public class OrderController : Controller
    {
        #region Variables and Properties

        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        

        #endregion Variables and Properties

        #region Constructors

        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
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

        [Authorize(Roles = Constant.KitchenUser + "," + Constant.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);
            if (orderHeader == null)
            {
                return NotFound();
            }
            orderHeader.Status = Constant.Status_InProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction(Constant.Action_ManageOrder, Constant.Controller_Order);
        }

        [Authorize(Roles = Constant.KitchenUser + "," + Constant.ManagerUser)]
        public async Task<IActionResult> OrderReady(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);
            if (orderHeader == null)
            {
                return NotFound();
            }
            orderHeader.Status = Constant.Status_Ready;
            await _db.SaveChangesAsync();

            // Email logic to notify customer that order is ready for pickup
            await _emailSender.SendEmailAsync(_db.ApplicationUser.FirstOrDefault(a => a.Id == orderHeader.UserId)?.Email
                    , Constant.Email_Title_OrderReady + " " + orderHeader.Id.ToString()
                    , Constant.Email_Subject_OrderReady);

            return RedirectToAction(Constant.Action_ManageOrder, Constant.Controller_Order);
        }

        [Authorize(Roles = Constant.KitchenUser + "," + Constant.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);
            if (orderHeader == null)
            {
                return NotFound();
            }
            orderHeader.Status = Constant.Status_Cancelled;
            await _db.SaveChangesAsync();

            // Email logic to notify customer that order canceled
            await _emailSender.SendEmailAsync(_db.ApplicationUser.FirstOrDefault(a => a.Id == orderHeader.UserId)?.Email
                    , Constant.Email_Title_OrderCanceled + " " + orderHeader.Id.ToString()
                    , Constant.Email_Subject_OrderCanceled);

            return RedirectToAction(Constant.Action_ManageOrder, Constant.Controller_Order);
        }

        #endregion Manage Order


        #region Order Pickup

        [Authorize(Roles = Constant.FrontDeskUser + "," + Constant.ManagerUser)]
        [HttpGet]
        public async Task<IActionResult> OrderPickup(int pageIndex = 1, string searchName = null, string searchPhone = null, string searchEmail = null)
        {
            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailViewModel>()
            };

            StringBuilder param = new StringBuilder();
            param.Append(string.Concat(
                  "/", Constant.Area_Customer,
                  "/", Constant.Controller_Order,
                  "/", Constant.Action_OrderPickup,
                  "?", Constant.Parameter_PageIndex,
                  ":"));

            param.Append($"&{Constant.Parameter_searchName}");
            if (!string.IsNullOrEmpty(searchName))
            {
                param.Append(searchName);
            }
            param.Append($"&{Constant.Parameter_searchPhone}");
            if (!string.IsNullOrEmpty(searchPhone))
            {
                param.Append(searchPhone);
            }
            param.Append($"&{Constant.Parameter_searchEmail}");
            if (!string.IsNullOrEmpty(searchEmail))
            {
                param.Append(searchEmail);
            }

            List<OrderHeader> orderHeaderList = new List<OrderHeader>();

            // get list order ready to pickup by keyword
            if ( !string.IsNullOrEmpty(searchName) || !string.IsNullOrEmpty(searchPhone) || !string.IsNullOrEmpty(searchEmail))
            {
                if (!string.IsNullOrEmpty(searchName))
                {
                    orderHeaderList = await _db.OrderHeader
                        .Include(o => o.ApplicationUser)
                        .Where(o => o.PickupName.ToLower().Contains(searchName) && o.Status == Constant.Status_Ready)
                        .ToListAsync();
                }
                else 
                {
                    if (!string.IsNullOrEmpty(searchPhone))
                    {
                        var user = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.PhoneNumber.Contains(searchPhone));
                        if(user != null)
                        {
                            orderHeaderList = await _db.OrderHeader
                            .Include(o => o.ApplicationUser)
                            .Where(o => o.UserId == user.Id && o.Status == Constant.Status_Ready)
                            .ToListAsync();
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(searchEmail))
                        {
                            var user = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Email.ToLower().Contains(searchEmail));
                            if(user != null)
                            {
                                orderHeaderList = await _db.OrderHeader
                                .Include(o => o.ApplicationUser)
                                .Where(o => o.UserId == user.Id && o.Status == Constant.Status_Ready)
                                .ToListAsync();
                            }
                        }
                    }
                }
            }
            else
            {
                orderHeaderList = await _db.OrderHeader
                    .Include(o => o.ApplicationUser)
                    .Where(o => o.Status == Constant.Status_Ready)
                    .ToListAsync();
            }

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
                urlParam = param.ToString()
            };

            return View(orderListVM);
        }

        [Authorize(Roles = Constant.FrontDeskUser + "," + Constant.ManagerUser)]
        [HttpPost, ActionName(Constant.Action_OrderPickup)]
        public async Task<IActionResult> OrderPickupPost(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);
            if (orderHeader == null)
            {
                return NotFound();
            }
            orderHeader.Status = Constant.Status_Completed;
            await _db.SaveChangesAsync();

            // Email logic to notify customer that order is completed
            await _emailSender.SendEmailAsync(_db.ApplicationUser.FirstOrDefault(a => a.Id == orderHeader.UserId)?.Email
                    , Constant.Email_Title_OrderCompleted + " " + orderHeader.Id.ToString()
                    , Constant.Email_Subject_OrderCompleted);

            return RedirectToAction(Constant.Action_OrderPickup, Constant.Controller_Order);
        }


        #endregion


        #endregion Methods
    }
}