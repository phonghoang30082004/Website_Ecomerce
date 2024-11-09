using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PayPal.Api;
using System.Security.Claims;
using WebAPP.Models;
using WebAPP.Models.Repository;
using WebAPP.Models.ViewModels;

namespace WebAPP.Controllers
{
    public class CheckOutOrderController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IConfiguration _configuration;
        private readonly FirebaseClient _firebaseClient;


        public CheckOutOrderController(DataContext context, IConfiguration configuration, FirebaseClient firebaseClient)
        {
            _dataContext = context;
            _configuration = configuration;
            _firebaseClient = firebaseClient;
        }
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _firebaseClient
                             .Child("CartItems")
                             .Child(userId)  // Giả sử dữ liệu giỏ hàng được lưu theo userId
                             .OnceAsync<CartItemModel>();

            var cartItemList = cartItems.Select(item => item.Object).ToList();
            cartItemList = cartItemList.OrderBy(item => item.StoreName).ToList();


            var user = _dataContext.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");

            }

            var shippingPriceCookie = Request.Cookies["ShippingPrice"];
            decimal shippingPrice = 0;
            if (shippingPriceCookie != null)
            {
                var shippingPriceJson = shippingPriceCookie;
                shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
            }

            decimal grandTotal = cartItemList.Sum(item => item.Quantity * item.Price) + shippingPrice;
       

            OrderViewModel cartVM = new()
            {
                UserName = user.UserName,
                Name = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Products = cartItemList,
                ShippingCost = shippingPrice,
                GrandTotal = grandTotal
            };
            
            
            Response.Cookies.Append("GrandTotal", cartVM.GrandTotal.ToString("F2"));

            return View(cartVM);
        }


        [HttpPost]
        public async Task<IActionResult> GetShipping(string quan, string tinh, string phuong)
        {
            var existingShipping = await _dataContext.Shippings
                .FirstOrDefaultAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

            decimal shippingPrice;

            if (existingShipping != null)
            {
                shippingPrice = existingShipping.Price;
            }
            else
            {
                shippingPrice = 50000; 
            }

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(10), 
                    Secure = true 
                };
            
                Response.Cookies.Append("ShippingPrice", shippingPrice.ToString(), cookieOptions); // Lưu giá trị là chuỗi

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _firebaseClient
                             .Child("CartItems")
                             .Child(userId)  // Giả sử dữ liệu giỏ hàng được lưu theo userId
                             .OnceAsync<CartItemModel>();

            var cartItemList = cartItems.Select(item => item.Object).ToList();

            // Tính tổng tiền (bao gồm phí vận chuyển)
            decimal grandTotal = cartItemList.Sum(x => x.Quantity * x.Price) + shippingPrice;
            HttpContext.Session.SetString("GrandTotal", grandTotal.ToString("F2"));

            return Json(new { success = true, shippingPrice = shippingPrice, grandTotal = grandTotal });
        }



        [HttpGet]
        public IActionResult DeleteShippingCost()
        {
            // Xóa cookie phí vận chuyển
            Response.Cookies.Delete("ShippingPrice");
            return RedirectToAction("Index", "Cart");
        }


        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            var coupon = _dataContext.Coupons.FirstOrDefault(c => c.Code == couponCode && c.IsActive);

            if (coupon == null)
            {
                TempData["CouponError"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Index");
            }

            // Lưu mã giảm giá vào session
            HttpContext.Session.SetString("CouponCode", couponCode);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _firebaseClient
                             .Child("CartItems")
                             .Child(userId)  // Giả sử dữ liệu giỏ hàng được lưu theo userId
                             .OnceAsync<CartItemModel>();

            var cartItemList = cartItems.Select(item => item.Object).ToList();


            // Tính toán GrandTotal từ giỏ hàng
            decimal grandTotal = cartItemList.Sum(x => x.Quantity * x.Price);

            // Lấy phí vận chuyển từ cookie
            decimal shippingPrice = 0;
            var shippingPriceCookie = Request.Cookies["ShippingPrice"];
            if (!string.IsNullOrEmpty(shippingPriceCookie))
            {
                shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceCookie);
            }

            grandTotal += shippingPrice;

            decimal discountedTotal = grandTotal * (1 - coupon.DiscountPercentage / 100);

            // Lưu giá trị GrandTotal vào session
            HttpContext.Session.SetString("GrandTotal", discountedTotal.ToString("F2"));

            // Trả về kết quả
            return Json(new
            {
                success = true,
                shippingPrice = shippingPrice,
                grandTotal = discountedTotal,
                message = "Mã giảm giá đã được áp dụng thành công!"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userName = Request.Form["UserName"];
            var name = Request.Form["Name"];
            var address = Request.Form["Address"];
            var phoneNumber = Request.Form["PhoneNumber"];
            var user = _dataContext.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string paymentMethod = Request.Form["PaymentMethod"];

            // Kiểm tra tên người dùng
            if (user.UserName == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderCode = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("OrderCode", orderCode);

            // Lấy các sản phẩm trong giỏ hàng từ Firebase
            var cartItems = await _firebaseClient
                                 .Child("CartItems")
                                 .Child(userId)  // Giả sử dữ liệu giỏ hàng được lưu theo userId
                                 .OnceAsync<CartItemModel>();

            var cartItemList = cartItems.Select(item => item.Object).ToList();

            if (!cartItemList.Any())  // Kiểm tra nếu giỏ hàng trống
            {
                TempData["error"] = "Giỏ hàng của bạn hiện đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            var shippingPriceCookie = Request.Cookies["ShippingPrice"];
            decimal shippingPrice = shippingPriceCookie != null ? JsonConvert.DeserializeObject<decimal>(shippingPriceCookie) : 0;

            // Lấy giá trị GrandTotal từ cookie
            var grandTotalString = HttpContext.Session.GetString("GrandTotal");
            decimal grandTotal = !string.IsNullOrEmpty(grandTotalString) && decimal.TryParse(grandTotalString, out var temp) ? temp : 0;

            // Nhóm các sản phẩm theo StoreName
            var groupedItems = cartItemList.GroupBy(item => item.StoreName); // Nhóm theo StoreName

            foreach (var group in groupedItems)
            {
                // Lấy thông tin từ Request.Form
                var orderItem = new OrderModel
                {
                    OrderCode = orderCode,
                    UserName = user.Email,
                    Status = 1,
                    CreatedDate = DateTime.Now,
                    UserId = user.Id,
                    GrandTotal = grandTotal, // Tính GrandTotal cho nhóm
                    ShippingCost = shippingPrice,
                    StoreName = group.Key, // Lấy tên cửa hàng từ khóa nhóm
                    Name = name, // Tên người nhận
                    Address = address, // Địa chỉ người nhận
                    PhoneNumber = phoneNumber // Số điện thoại người nhận
                };

                _dataContext.Orders.Add(orderItem);
                await _dataContext.SaveChangesAsync();

                foreach (var cart in group)
                {
                    var orderDetails = new OrderDetails
                    {
                        UserName = user.UserName,
                        OrderCode = orderCode,
                        ProductId = cart.ProductID,
                        Price = cart.Price,
                        Quantity = cart.Quantity,
                        GrandTotal = cart.Quantity * cart.Price,
                        StoreName = group.Key, // Lưu StoreName từ khóa nhóm
                        OrderId = orderItem.Id // Liên kết với OrderModel tương ứng của nhóm
                    };

                    // Cập nhật số lượng và trạng thái bán hàng của sản phẩm
                    var product = await _dataContext.Products.Where(p => p.Id == cart.ProductID).FirstOrDefaultAsync();
                    if (product != null)
                    {
                        product.Quantity -= cart.Quantity; // Giảm số lượng sản phẩm
                        product.Sold += cart.Quantity; // Cập nhật số lượng đã bán
                        _dataContext.Products.Update(product);
                    }

                    // Lưu OrderDetails
                    _dataContext.OrderDetails.Add(orderDetails);
                    await _dataContext.SaveChangesAsync();
                }
            }

            // Xử lý thanh toán
            if (paymentMethod == "PayPal")
            {
                HttpContext.Session.SetString("OrderCode", orderCode);
                HttpContext.Session.SetString("UserEmail", userEmail);
                HttpContext.Session.SetString("UserId", userId);
                HttpContext.Session.SetString("ShippingCost", shippingPrice.ToString());

                // Xóa giỏ hàng và cookie
                await ClearCartInFirebase(userId); // Gọi phương thức để xóa giỏ hàng khỏi Firebase
                HttpContext.Session.Remove("CouponCode");
                Response.Cookies.Delete("GrandTotal");
                Response.Cookies.Delete("ShippingPrice");

                // Gọi hàm tạo thanh toán
                return CreatePayment(orderCode, grandTotal);
            }

            // Xóa giỏ hàng và cookie
            await ClearCartInFirebase(userId); // Gọi phương thức để xóa giỏ hàng khỏi Firebase
            HttpContext.Session.Remove("CouponCode");
            Response.Cookies.Delete("GrandTotal");
            Response.Cookies.Delete("ShippingPrice");

            TempData["success"] = "Đơn hàng đã được tạo thành công!";
            return RedirectToAction("Index", "Cart");
        }

        private async Task ClearCartInFirebase(string userId)
        {
            var cartItems = await _firebaseClient
                        .Child("CartItems")
                        .Child(userId)
                        .OnceAsync<CartItemModel>();

            foreach (var cartItem in cartItems)
            {
                await _firebaseClient.Child("CartItems").Child(userId).Child(cartItem.Key).DeleteAsync();
            }
        }

        public IActionResult CreatePayment(string orderCode, decimal grandTotal)
        {
            // Khởi tạo APIContext
            var apiContext = new APIContext(new OAuthTokenCredential(
                _configuration["PayPal:ClientId"],
                _configuration["PayPal:ClientSecret"]
            ).GetAccessToken())
            {
                Config = new Dictionary<string, string>
        {
            { "mode", _configuration["PayPal:Mode"] } // 'sandbox' hoặc 'live'
        }
            };

            var payment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" },
                transactions = new List<Transaction>
             {
            new Transaction
            {
                description = "Order payment",
                invoice_number = orderCode,
                amount = new Amount
                {
                    currency = "USD", // Đổi sang loại tiền tệ nếu cần
                    total = grandTotal.ToString("F2")
                }
            }
        },
                redirect_urls = new RedirectUrls
                {
                    cancel_url = Url.Action("Cancel", "Checkout", null, Request.Scheme),
                    return_url = Url.Action("Success", "Checkout", null, Request.Scheme)
                }
            };

            var createdPayment = payment.Create(apiContext);
            return Redirect(createdPayment.links.First(l => l.rel == "approval_url").href);
        }

        public async Task<IActionResult> Success(string paymentId, string token)
        {
            var orderCode = HttpContext.Session.GetString("OrderCode");

            if (string.IsNullOrEmpty(orderCode))
            {
                return NotFound("Đơn hàng không tồn tại.");
            }

            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null)
            {
                return NotFound("Đơn hàng không tồn tại.");
            }

            order.Status = 4;
            await _dataContext.SaveChangesAsync();

            // Xóa thông tin khỏi session
            HttpContext.Session.Remove("OrderCode");
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("ShippingCost");

            TempData["success"] = "Thanh toán thành công!";
            return RedirectToAction("Index", "Cart");
        }





    }
}
