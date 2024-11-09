﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPalCheckoutSdk.Orders;
using System.Security.Claims;
using WebAPP.Models;
using WebAPP.Models.Repository;
using WebAPP.Models.ViewModels;

namespace WebAPP.Controllers
{
	public class AccountController : Controller
	{
		private UserManager<AppUserModel> _userManager;
		private SignInManager<AppUserModel> _signManager;
        private readonly DataContext _dataContext;

        public AccountController(UserManager<AppUserModel> userManager, SignInManager<AppUserModel> signManager, DataContext dataContext)
		{
			_userManager = userManager;
			_signManager = signManager;
            _dataContext = dataContext;
		}

		public IActionResult Login(string returnUrl)
		{
			return View(new LoginViewModel { ReturnUrl = returnUrl });
		}

		public IActionResult NguoiBanHang()
		{
			return View(); 
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel loginvm)
		{

			if (ModelState.IsValid)
			{
				Microsoft.AspNetCore.Identity.SignInResult result = await _signManager.PasswordSignInAsync(loginvm.UserName, loginvm.Password, false, false);
				if (result.Succeeded)
				{
					return Redirect(loginvm.ReturnUrl ?? "/"); //khoong thi tra ve trang chinh
				}
				ModelState.AddModelError("", "Tài khoản hoặc mật khẩu sai");
				return View(loginvm);
			}
			return View();
		}

		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(UserModel user)
		{
			if (ModelState.IsValid)
			{
                AppUserModel newUser = new AppUserModel
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FullName = user.FullName,
                    Address = user.Address,
                    Gender = user.Gender,
                    BirthYear = user.BirthYear,
                };

                // Tạo tài khoản người dùng
                IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);
                if (result.Succeeded)
				{
					TempData["success"] = "Tạo User Thành Công";
					return Redirect("/Account/Login");
				}
				foreach (IdentityError error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}

			return View();
		}

        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                BirthYear = user.BirthYear,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    user.FullName = model.FullName;
                    user.Address = model.Address;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Gender = model.Gender;
                    user.BirthYear = model.BirthYear;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        TempData["success"] = "Cập nhật thông tin thành công";
                        return RedirectToAction("Index", "Home");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(model);
        }


        public async Task<IActionResult> OrderList()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Lấy tất cả đơn hàng của người dùng, sau đó chọn ra các order code duy nhất
            var orders = await _dataContext.Orders
                                    .Where(o => o.UserId == userId)
                                    .ToListAsync();

            // Nhóm theo OrderCode và lấy một đơn hàng từ mỗi nhóm
            var uniqueOrders = orders
                               .GroupBy(o => o.OrderCode)
                               .Select(g => g.FirstOrDefault())
                               .Select(o => new
                               {
                                   o.OrderCode,
                                   o.CreatedDate,
                                   o.Status,
                                   o.GrandTotal
                               })
                               .ToList();

            return View(uniqueOrders);
        }

        // Phương thức để xem chi tiết đơn hàng
        public async Task<IActionResult> ViewOrder(string orderCode)
        {
            // Lấy thông tin đơn hàng từ bảng Order dựa trên orderCode và UserId
            var order = await _dataContext.Orders
                                           .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kiểm tra xem đơn hàng có tồn tại và có thuộc về người dùng hiện tại không
            if (order == null)
            {
                return NotFound();
            }

            // Lấy danh sách chi tiết sản phẩm của đơn hàng
            var orderDetails = await _dataContext.OrderDetails
                                                 .Include(od => od.Products)
                                                 .Where(od => od.OrderCode == orderCode)
                                                 .ToListAsync();

            // Truyền các thông tin cần thiết vào ViewBag
            ViewBag.ShippingCost = order.ShippingCost;
            ViewBag.GrandTotal = order.GrandTotal;
            ViewBag.OrderStatus = order.Status;

            // Trả về view với danh sách các sản phẩm của đơn hàng
            return View(orderDetails);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _dataContext.Orders.FindAsync(id);

            if (order == null || order.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound();
            }

            if (order.Status != 0)
            {
                return BadRequest("Không thể hủy đơn hàng này.");
            }

            order.Status = 2;
            await _dataContext.SaveChangesAsync();

            return RedirectToAction("ViewOrder", new { id = order.Id });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(string orderCode, string value)
        {
            if (string.IsNullOrEmpty(orderCode))
            {
                ModelState.AddModelError(string.Empty, "Mã đơn hàng không hợp lệ.");
                return RedirectToAction("OrderList");
            }

            var order = await _dataContext.Orders
                                           .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (order == null)
            {
                return NotFound();
            }

            // Chuyển đổi `value` từ `string` sang `int` và gán cho `order.Status`
            if (int.TryParse(value, out int statusValue))
            {
                order.Status = statusValue;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Giá trị trạng thái không hợp lệ.");
                return RedirectToAction("OrderList");
            }

            await _dataContext.SaveChangesAsync();

            return RedirectToAction("OrderList");
        }




        public async Task<IActionResult> Logout()
		{
			await _signManager.SignOutAsync();
			return RedirectToAction("Index", "Home", new { area = "" });
		}
	}
}
