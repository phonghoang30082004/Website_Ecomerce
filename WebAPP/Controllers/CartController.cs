using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAPP.Models;
using WebAPP.Models.Repository;
using WebAPP.Models.ViewModels;

namespace WebAPP.Controllers
{
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly FirebaseClient _firebaseClient;


        public CartController(DataContext context, IConfiguration configuration)
        {
            _dataContext = context;

            var googleCredential = GoogleCredential.FromFile(configuration["Firebase:CredentialsPath"])
     .CreateScoped("https://www.googleapis.com/auth/firebase.database"); // Đảm bảo rằng bạn sử dụng phạm vi cần thiết cho Firebase

            _firebaseClient = new FirebaseClient(
                configuration["Firebase:DatabaseUrl"],
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = async () => await googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync()
                });
        }


        // Hiển thị giỏ hàng
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Bạn cần đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }
            var cartItems = await _firebaseClient
                    .Child("CartItems")
                    .Child(userId)
                    .OnceAsync<CartItemModel>();

            var cartItemList = cartItems.Select(item => item.Object).ToList();

            // Sắp xếp cartItems theo StoreName
            cartItemList = cartItemList.OrderBy(item => item.StoreName).ToList();

            

            CartItemViewModel cartVM = new()
            {
                CartItems = cartItemList,
                GrandTotal = cartItemList.Sum(x => x.Quantity * x.Price)
            };

            
            

            var grandTotalJson = JsonConvert.SerializeObject(cartVM.GrandTotal);
            Response.Cookies.Append("GrandTotal", grandTotalJson, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                HttpOnly = true,
                Secure = true
            });

            return View(cartVM);
        }



        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> Add(int Id, int? Quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Quantity == null || Quantity <= 0)
            {
                Quantity = 1; // Mặc định nếu không có giá trị quantity
            }
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy sản phẩm từ SQL
            ProductModel product = await _dataContext.Products.Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == Id);
            if (product == null)
            {
                TempData["error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index");
            }

            // Lấy giỏ hàng của người dùng từ Firebase
            var cartItems = await _firebaseClient
                .Child("CartItems")
                .Child(userId)
                .OnceAsync<CartItemModel>();

            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var existingCartItem = cartItems.FirstOrDefault(c => c.Object.ProductID == Id);

            if (existingCartItem != null)
            {
                // Nếu sản phẩm đã có trong giỏ hàng, tăng số lượng
                existingCartItem.Object.Quantity += Quantity.Value;

                // Cập nhật lại sản phẩm trong giỏ hàng trên Firebase
                await _firebaseClient
                    .Child("CartItems")
                    .Child(userId)
                    .Child(existingCartItem.Key)
                    .PutAsync(existingCartItem.Object);

                TempData["success"] = "Sản phẩm đã được cập nhật số lượng!";
            }
            else
            {
                // Nếu sản phẩm chưa có trong giỏ hàng, thêm sản phẩm mới
                CartItemModel cartItem = new CartItemModel
                {
                    ProductID = product.Id,
                    ProductName = product.Name,
                    Quantity = Quantity ?? 1,
                    Price = product.Price,
                    Image = product.Image,
                    SupplierId = product.SupplierId ?? 0,
                    StoreName = product.Supplier?.StoreName
                };

                // Lưu sản phẩm mới vào Firebase
                await _firebaseClient
                    .Child("CartItems")
                    .Child(userId)  // Dùng userId làm con đường lưu trữ
                    .PostAsync(cartItem);

                TempData["success"] = "Thêm sản phẩm vào giỏ hàng thành công!";
            }

            return RedirectToAction("Index");
        }


        // Giảm số lượng sản phẩm trong giỏ hàng
        public async Task<IActionResult> Decrease(int Id)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _firebaseClient
                .Child("CartItems")
                .Child(userId)
                .OnceAsync<CartItemModel>();

            var cartItem = cartItems.FirstOrDefault(c => c.Object.ProductID == Id);

            if (cartItem != null)
            {
                // Giảm số lượng sản phẩm trong giỏ hàng
                if (cartItem.Object.Quantity > 1)
                {
                    cartItem.Object.Quantity -= 1;
                }
                else
                {
                    // Nếu số lượng còn lại <= 1, xóa sản phẩm khỏi giỏ hàng
                    await _firebaseClient.Child("CartItems").Child(userId).Child(cartItem.Key).DeleteAsync();
                }

                // Cập nhật lại giỏ hàng trên Firebase (nếu số lượng vẫn còn > 0)
                if (cartItem.Object.Quantity > 0)
                {
                    await _firebaseClient.Child("CartItems").Child(userId).Child(cartItem.Key).PutAsync(cartItem.Object);
                }
            }

            return RedirectToAction("Index");
        }

        // Tăng số lượng sản phẩm trong giỏ hàng
        public async Task<IActionResult> Increase(int Id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _firebaseClient
                .Child("CartItems")
                .Child(userId)
                .OnceAsync<CartItemModel>();

            var cartItem = cartItems.FirstOrDefault(c => c.Object.ProductID == Id);

            if (cartItem != null)
            {
                var product = await _dataContext.Products.FirstOrDefaultAsync(p => p.Id == Id);
                if (product != null)
                {
                    // Kiểm tra số lượng trong giỏ hàng so với số lượng tồn kho
                    if (cartItem.Object.Quantity < product.Quantity)
                    {
                        // Tăng số lượng nếu trong giỏ hàng nhỏ hơn số lượng tồn kho
                        cartItem.Object.Quantity+=1;
                        TempData["success"] = "Tăng số lượng thành công!";
                        await _firebaseClient.Child("CartItems").Child(userId).Child(cartItem.Key).PutAsync(cartItem.Object);
                    }
                    else
                    {
                        // Không tăng nếu giỏ hàng đã đủ số lượng tồn kho
                        TempData["error"] = "Sản phẩm đã đạt số lượng tối đa cho phép!";

                    }

                  
                    
                }
            }

            return RedirectToAction("Index");
        }


        // Xóa sản phẩm khỏi giỏ hàng
        public async Task<IActionResult> Remove(int Id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _firebaseClient
                .Child("CartItems")
                .Child(userId)
                .OnceAsync<CartItemModel>();

            var cartItem = cartItems.FirstOrDefault(c => c.Object.ProductID == Id);
            if (cartItem != null)
            {
                // Xóa sản phẩm khỏi Firebase
                await _firebaseClient.Child("CartItems").Child(userId).Child(cartItem.Key).DeleteAsync();
            }

            return RedirectToAction("Index");
        }

        // Xóa toàn bộ giỏ hàng
        public async Task<IActionResult> Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _firebaseClient
                         .Child("CartItems")
                         .Child(userId)
                         .OnceAsync<CartItemModel>();

            foreach (var cartItem in cartItems)
            {
                await _firebaseClient.Child("CartItems").Child(userId).Child(cartItem.Key).DeleteAsync();
            }
            return RedirectToAction("Index");
        }


   
    }
}
