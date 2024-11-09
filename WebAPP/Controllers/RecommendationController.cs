using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPP.Models;
using WebAPP.Models.Repository;
using WebAPP.Models.ViewModels;
using WebAPP.Services;


namespace WebAPP.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly ProductRecommendationService _recommendationService;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly DataContext _dataContext;



        public RecommendationController(ProductRecommendationService recommendationService , UserManager<AppUserModel> userManager, DataContext dataContext)
        {
            _recommendationService = recommendationService;
            _userManager = userManager;
            _dataContext = dataContext;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); // Trả về Unauthorized nếu người dùng không đăng nhập
            }
            string userId = user.Id;

            int nRecommendations = 2;
            List<int> recommendationIds = await _recommendationService.GetRecommendationsAsync(userId, nRecommendations);

            // Lấy các sản phẩm gợi ý từ ID sản phẩm
            var recommendedProducts = await _dataContext.Products
                .Where(p => recommendationIds.Contains(p.Id))
                .ToListAsync();

            // Phân trang danh sách sản phẩm chính
            int itemsPerPage = 10; // Số lượng sản phẩm trên mỗi trang
            int totalItems = await _dataContext.Products.CountAsync(); // Tổng số sản phẩm
            var paginatedProducts = await _dataContext.Products
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            var viewModel = new ProductListViewModel
            {
                Products = new PaginatedList<ProductModel>(paginatedProducts, totalItems, page, itemsPerPage), // Danh sách sản phẩm chính với phân trang
                RecommendedProducts = recommendedProducts, // Danh sách sản phẩm gợi ý
                PagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    TotalItems = totalItems,
                    ItemsPerPage = itemsPerPage
                }
            };

            return View(viewModel);
        }





    }
}
