using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebAPP.Models;
using WebAPP.Models.Repository;
using WebAPP.Models.ViewModels;
using WebAPP.Services;

namespace WebAPP.Controllers
{
    public class HomeController : Controller
    {
        public int PageSize = 6;
        private readonly ProductRecommendationService _recommendationService;
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly ILogger<HomeController> _logger;



        public HomeController(ILogger<HomeController> logger, DataContext context, UserManager<AppUserModel> userManager, ProductRecommendationService recommendationService)
        {
            _logger = logger;
            _dataContext = context;
            _userManager = userManager;
            _recommendationService= recommendationService;
        }


            public async Task<IActionResult> Index(string searchString, string searchName, decimal? minPrice, decimal? maxPrice, int pageIndex = 1)
            {
                int pageSize = PageSize;
                var products = _dataContext.Products
                                            .Include(p => p.Category)
                                            .Include(p => p.Brand)
                                            .AsNoTracking();

                if (!string.IsNullOrEmpty(searchString))
                {
                    products = products.Where(p => p.Name.Contains(searchString) || p.Description.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(searchName))
                {
                    products = products.Where(p => p.Name.Contains(searchName));
                }

                if (minPrice.HasValue)
                {
                    products = products.Where(p => p.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    products = products.Where(p => p.Price <= maxPrice.Value);
                }

                var paginatedList = await PaginatedList<ProductModel>.CreateAsync(products, pageIndex, pageSize);

                // Lấy thông tin người dùng
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(); // Trả về Unauthorized nếu người dùng không đăng nhập
                }
                string userId = user.Id;

                int nRecommendations = 3;
                List<int> recommendationIds = await _recommendationService.GetRecommendationsAsync(userId, nRecommendations);

                // Lấy các sản phẩm gợi ý từ ID sản phẩm
                var recommendedProducts = await _dataContext.Products
                    .Where(p => recommendationIds.Contains(p.Id))
                    .ToListAsync();

                var viewModel = new ProductListViewModel
                {
                    Products = paginatedList,
                    RecommendedProducts = recommendedProducts, // Danh sách sản phẩm gợi ý
                    PagingInfo = new PagingInfo
                    {
                        CurrentPage = pageIndex,
                        TotalItems = await products.CountAsync(),
                        ItemsPerPage = pageSize
                    }
                };

                ViewData["CurrentFilter"] = searchString;
                ViewData["CurrentNameFilter"] = searchName;
                ViewData["CurrentMinPriceFilter"] = minPrice;
                ViewData["CurrentMaxPriceFilter"] = maxPrice;

                var sliders = _dataContext.Sliders.Where(s => s.Status == 1).ToList();
                ViewBag.Sliders = sliders;

                return View(viewModel);
            }

        
        //Lấy danh sách wishlist
        public async Task<IActionResult> GetWishlist()
        {
            var user = await _userManager.GetUserAsync(User);
            var wishlistItems = await _dataContext.Wishlists
            .Include(w => w.Product) 
            .Where(p => p.UserId == user.Id)
            .ToListAsync();
            return View(wishlistItems);
        }
        [HttpPost]
        //phương thức thêm sản phẩm vào wishlist 
        public async Task<IActionResult> AddWishlist(int Id, WishlistModel wishlist)
        {
            var user = await _userManager.GetUserAsync(User);
            var wishlistModel = new WishlistModel
            {
                ProductId = Id,
                UserId = user.Id,
            };
            _dataContext.Add(wishlistModel);
            try
            {
                await _dataContext.SaveChangesAsync();
                return View();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while adding to wishlist");
            }
        }
        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(int productID)
        {
            var user = await _userManager.GetUserAsync(User);
            var removedItem = await _dataContext.Wishlists.
            FirstOrDefaultAsync(p => p.UserId == user.Id && p.ProductId == productID);
            if (removedItem == null)
            {
 
                return NotFound("The item was not found in your wishlist.");
            }

            _dataContext.Wishlists.Remove(removedItem);
            await _dataContext.SaveChangesAsync();
         
            return RedirectToAction("GetWishlist"); // Điều hướng về trang wishlist hoặc trang khác
        }
        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statuscode)
        {
            if (statuscode == 404)
            {
                return View("NotFound");
            }
            else
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }
    }
}
