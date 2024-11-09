using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPP.Models;
using WebAPP.Models.Repository;
using WebAPP.Models.ViewModels;

namespace WebAPP.Controllers
{
	public class BrandController : Controller
	{
		private readonly DataContext _dataContext;
		public BrandController(DataContext context)
		{
			_dataContext = context;
		}
		public async Task<IActionResult> Index(string Slug = " ", string sort_by = "", string startprice = "", string endprice = "", int pageIndex = 1, int pageSize = 6)
        
		{
			BrandModel brand = _dataContext.Brands.Where(c => c.Slug == Slug).FirstOrDefault();
			if (brand == null) return RedirectToAction("Index");

            IQueryable<ProductModel> productByBrand = _dataContext.Products.Where(c => c.BrandId == brand.Id);

            if (!string.IsNullOrEmpty(sort_by))
            {
                if (sort_by == "price_increase")
                {
                    productByBrand = productByBrand.OrderBy(c => c.Price);
                }
                else if (sort_by == "price_decrease")
                {
                    productByBrand = productByBrand.OrderByDescending(c => c.Price);
                }
                else if (sort_by == "price_newest")
                {
                    productByBrand = productByBrand.OrderByDescending(c => c.Id);
                }
                else if (sort_by == "price_oldest")
                {
                    productByBrand = productByBrand.OrderBy(c => c.Id);
                }
            }

            if (!string.IsNullOrEmpty(startprice) && !string.IsNullOrEmpty(endprice))
            {
                decimal startPriceValue, endPriceValue;
                if (decimal.TryParse(startprice, out startPriceValue) && decimal.TryParse(endprice, out endPriceValue))
                {
                    productByBrand = productByBrand.Where(p => p.Price >= startPriceValue && p.Price <= endPriceValue);
                }
            }

            int count = await productByBrand.CountAsync();
            if (count > 0)
            {
                decimal minPrice = await productByBrand.MinAsync(p => p.Price);
                decimal maxPrice = await productByBrand.MaxAsync(p => p.Price);

                ViewBag.sort_key = sort_by;
                ViewBag.count = count;
                ViewBag.minprice = minPrice;
                ViewBag.maxprice = maxPrice;
            }

            var paginatedList = await PaginatedList<ProductModel>.CreateAsync(productByBrand, pageIndex, pageSize);
            return View(paginatedList);
		}
	}
}
