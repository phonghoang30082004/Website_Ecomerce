using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPP.Models;
using WebAPP.Models.Repository;
using WebAPP.Models.ViewModels;

namespace WebAPP.Controllers
{
	public class CategoryController : Controller
	{
		private readonly DataContext _dataContext;
		public CategoryController(DataContext context)
		{
			_dataContext = context;
		}
        public async Task<IActionResult> Index(string Slug = " ", string sort_by = "", string startprice = "", string endprice = "", int pageIndex = 1, int pageSize = 6)
        {
            CategoryModel category = _dataContext.Categories.Where(c => c.Slug == Slug).FirstOrDefault();
            if (category == null) return RedirectToAction("Index");

            IQueryable<ProductModel> productByCategory = _dataContext.Products.Where(c => c.CategoryId == category.Id);

            if (!string.IsNullOrEmpty(sort_by))
            {
                if (sort_by == "price_increase")
                {
                    productByCategory = productByCategory.OrderBy(c => c.Price);
                }
                else if (sort_by == "price_decrease")
                {
                    productByCategory = productByCategory.OrderByDescending(c => c.Price);
                }
                else if (sort_by == "price_newest")
                {
                    productByCategory = productByCategory.OrderByDescending(c => c.Id);
                }
                else if (sort_by == "price_oldest")
                {
                    productByCategory = productByCategory.OrderBy(c => c.Id);
                }
            }

            if (!string.IsNullOrEmpty(startprice) && !string.IsNullOrEmpty(endprice))
            {
                decimal startPriceValue, endPriceValue;
                if (decimal.TryParse(startprice, out startPriceValue) && decimal.TryParse(endprice, out endPriceValue))
                {
                    productByCategory = productByCategory.Where(p => p.Price >= startPriceValue && p.Price <= endPriceValue);
                }
            }

            int count = await productByCategory.CountAsync();
            if (count > 0)
            {
                decimal minPrice = await productByCategory.MinAsync(p => p.Price);
                decimal maxPrice = await productByCategory.MaxAsync(p => p.Price);

                ViewBag.sort_key = sort_by;
                ViewBag.count = count;
                ViewBag.minprice = minPrice;
                ViewBag.maxprice = maxPrice;
            }

            var paginatedList = await PaginatedList<ProductModel>.CreateAsync(productByCategory, pageIndex, pageSize);
            return View(paginatedList);
        }

    }
}
