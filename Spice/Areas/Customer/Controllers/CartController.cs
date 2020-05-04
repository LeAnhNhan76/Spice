using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Common;
using Spice.Common.MethodStatic;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area(Constant.Area_Customer)]
    public class CartController : Controller
    {
        #region Variables and Properties

        private readonly ApplicationDbContext _db;

        [BindProperty]
        public OrderDetailCartViewModel OrderDetailCartVM { get; set; }

        #endregion Variables and Properties

        #region Constructors

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion Constructors

        #region Methods

        #region List

        public async Task<IActionResult> Index()
        {
            OrderDetailCartVM = new OrderDetailCartViewModel()
            {
                OrderHeader = new Models.OrderHeader()
            };

            OrderDetailCartVM.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value);

            if (cart != null)
            {
                OrderDetailCartVM.ShoppingCart = cart.ToList();
            }

            foreach (var item in OrderDetailCartVM.ShoppingCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetailCartVM.OrderHeader.OrderTotal += (item.MenuItem.Price * item.Count);
                item.MenuItem.Description = MethodStatic.ConvertToRawHtml(item.MenuItem.Description);
                if (item.MenuItem.Description.Length > 100)
                {
                    item.MenuItem.Description = item.MenuItem.Description.Substring(0, 99) + "...";
                }
            }
            OrderDetailCartVM.OrderHeader.OrderTotalOriginal = OrderDetailCartVM.OrderHeader.OrderTotal;

            //Coupon Code
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(Constant.Session_CouponCode)))
            {
                OrderDetailCartVM.OrderHeader.CouponCode = HttpContext.Session.GetString(Constant.Session_CouponCode);
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower() == OrderDetailCartVM.OrderHeader.CouponCode);
                OrderDetailCartVM.OrderHeader.OrderTotal = MethodStatic.DiscountedPrice(couponFromDb, OrderDetailCartVM.OrderHeader.OrderTotalOriginal);
            }

            return View(OrderDetailCartVM);
        }

        #endregion List

        #region Summary

        public async Task<IActionResult> Summary()
        {
            OrderDetailCartVM = new OrderDetailCartViewModel()
            {
                OrderHeader = new Models.OrderHeader()
            };

            OrderDetailCartVM.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == claim.Value);

            var cart = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value);

            if (cart != null)
            {
                OrderDetailCartVM.ShoppingCart = cart.ToList();
            }

            foreach (var item in OrderDetailCartVM.ShoppingCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetailCartVM.OrderHeader.OrderTotal += (item.MenuItem.Price * item.Count);
            }
            OrderDetailCartVM.OrderHeader.OrderTotalOriginal = OrderDetailCartVM.OrderHeader.OrderTotal;
            OrderDetailCartVM.OrderHeader.PickupName = applicationUser.Name;
            OrderDetailCartVM.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            OrderDetailCartVM.OrderHeader.PickupTime = DateTime.Now;

            //Coupon Code
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(Constant.Session_CouponCode)))
            {
                OrderDetailCartVM.OrderHeader.CouponCode = HttpContext.Session.GetString(Constant.Session_CouponCode);
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower() == OrderDetailCartVM.OrderHeader.CouponCode);
                OrderDetailCartVM.OrderHeader.OrderTotal = MethodStatic.DiscountedPrice(couponFromDb, OrderDetailCartVM.OrderHeader.OrderTotalOriginal);
            }

            return View(OrderDetailCartVM);
        }

        [HttpPost, ActionName(Constant.Action_Summary)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPOST(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailCartVM.ShoppingCart = await _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToListAsync();

            OrderDetailCartVM.OrderHeader.Status = Constant.Status_Submitted;
            OrderDetailCartVM.OrderHeader.PaymentStatus = Constant.PaymentStatus_Pending;
            OrderDetailCartVM.OrderHeader.UserId = claim.Value;
            OrderDetailCartVM.OrderHeader.OrderDate = DateTime.Now;
            OrderDetailCartVM.OrderHeader.PickupDate = Convert.ToDateTime(OrderDetailCartVM.OrderHeader.PickupDate.ToShortDateString() + " " + OrderDetailCartVM.OrderHeader.PickupTime.ToShortTimeString());

            List<OrderDetail> orderDetailList = new List<OrderDetail>();
            _db.OrderHeader.Add(OrderDetailCartVM.OrderHeader);
            await _db.SaveChangesAsync();

            OrderDetailCartVM.OrderHeader.OrderTotalOriginal = 0;

            foreach (var item in OrderDetailCartVM.ShoppingCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetail orderDetail = new OrderDetail()
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = OrderDetailCartVM.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                OrderDetailCartVM.OrderHeader.OrderTotalOriginal += orderDetail.Count * orderDetail.Price;
                orderDetailList.Add(orderDetail);
            }
            _db.OrderDetail.AddRange(orderDetailList);

            //Coupon Code
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(Constant.Session_CouponCode)))
            {
                OrderDetailCartVM.OrderHeader.CouponCode = HttpContext.Session.GetString(Constant.Session_CouponCode);
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower() == OrderDetailCartVM.OrderHeader.CouponCode.ToLower());
                OrderDetailCartVM.OrderHeader.OrderTotal = MethodStatic.DiscountedPrice(couponFromDb, OrderDetailCartVM.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                OrderDetailCartVM.OrderHeader.OrderTotal = OrderDetailCartVM.OrderHeader.OrderTotalOriginal;
            }
            OrderDetailCartVM.OrderHeader.CouponCodeDiscount = OrderDetailCartVM.OrderHeader.OrderTotalOriginal - OrderDetailCartVM.OrderHeader.OrderTotal;
            _db.ShoppingCart.RemoveRange(OrderDetailCartVM.ShoppingCart);
            HttpContext.Session.SetInt32(Constant.Session_CartCount, 0);
            await _db.SaveChangesAsync();

            var chargeOption = new ChargeCreateOptions()
            {
                Amount = Convert.ToInt32(OrderDetailCartVM.OrderHeader.OrderTotal * 100),
                Currency = Constant.Currency_USD,
                Description = "Order ID: " + OrderDetailCartVM.OrderHeader.Id,
                Source = stripeToken
            };

            var chargeService = new ChargeService();
            var charge = chargeService.Create(chargeOption);

            if (charge.BalanceTransactionId == null)
            {
                OrderDetailCartVM.OrderHeader.PaymentStatus = Constant.PaymentStatus_Rejected;
            }
            else
            {
                OrderDetailCartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == Constant.ActionResult_Succeeded)
            {
                OrderDetailCartVM.OrderHeader.PaymentStatus = Constant.PaymentStatus_Approved;
                OrderDetailCartVM.OrderHeader.Status = Constant.Status_Submitted;
            }
            else
            {
                OrderDetailCartVM.OrderHeader.PaymentStatus = Constant.PaymentStatus_Rejected;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(Constant.Action_Confirm, Constant.Controller_Order, new { id = OrderDetailCartVM.OrderHeader.Id });
        }

        #endregion Summary

        #region Coupon

        public IActionResult AddCoupon()
        {
            if (OrderDetailCartVM.OrderHeader.CouponCode == null)
            {
                OrderDetailCartVM.OrderHeader.CouponCode = "";
            }
            HttpContext.Session.SetString(Constant.Session_CouponCode, OrderDetailCartVM.OrderHeader.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(Constant.Session_CouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        #endregion Coupon

        #region Plus && Minus && Remove

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(s => s.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(s => s.Id == cartId);
            if (cart.Count == 1)
            {
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();
                var countCart = _db.ShoppingCart.Where(s => s.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(Constant.Session_CartCount, countCart);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(s => s.Id == cartId);
            _db.ShoppingCart.Remove(cart);
            await _db.SaveChangesAsync();

            var countCart = _db.ShoppingCart.Where(s => s.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(Constant.Session_CartCount, countCart);

            return RedirectToAction(nameof(Index));
        }

        #endregion Plus && Minus && Remove

        #endregion Methods
    }
}