using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPP.Models;
using WebAPP.Models.Repository;

namespace WebAPP.Controllers
{
    public class SellerController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<AppUserModel> _userManager;

        public SellerController(DataContext context, IWebHostEnvironment webHostEnvironment, UserManager<AppUserModel>userManager)
        {
            _dataContext = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }
        public async  Task<IActionResult> Index()
        {
            // Lấy UserId của người dùng hiện tại

            return View();
        }

        public async Task<IActionResult> ManagerProduct() 
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var products = await _dataContext.Products.Where(p => p.UserId == userId).Include(p => p.Brand).Include(p => p.Category).ToListAsync();


            return View(products); 
        }
     

        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name");
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product)
        {

            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name");
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name");
            var user = await _userManager.GetUserAsync(User);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await _dataContext.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);


            product.UserId = userId;

            if (ModelState.IsValid)
            {
                product.Supplier = new Supplier(); // Khởi tạo đối tượng Supplier
                if (supplier != null) // Chỉ khởi tạo nếu có nhà cung cấp
                {
                    
                    product.Supplier.StoreName = supplier.StoreName; 
                    product.SupplierId = supplier.Id; 
                }
                else
                {
                    // Có thể thêm logic để xử lý trường hợp không có nhà cung cấp, ví dụ:
                    ModelState.AddModelError("", "Người dùng chưa đăng ký là nhà cung cấp.");
                    return View(product); // Trả lại view với thông báo lỗi
                }
                if (product.ImageUpload != null)
                {
                    //detele
                    string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");

                    string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;

                    string filepath = Path.Combine(uploadDir, imageName);

                    FileStream fs = new FileStream(filepath, FileMode.Create);
                    await product.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                    product.Image = imageName;

                }

                _dataContext.Products.Add(product);
                await _dataContext.SaveChangesAsync();

                return RedirectToAction("ManagerProduct");
            }

            return View(product);
        }
        //Thêm danh mục sản phẩm
        public async Task<IActionResult> CreateCategory()
        {
            var user = await _userManager.GetUserAsync(User);
            //Lấy danh sách danh mục sản phẩm để truyền vào CreateCategory
            var categories = _dataContext.Categories.Where(c => c.UserId == user.Id).ToList() ?? new List<CategoryModel>(); // Lấy danh sách danh mục từ cơ sở dữ liệu
            return View(categories); // Truyền danh sách vào view
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(string categoryName, string categoryDescription)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!string.IsNullOrEmpty(categoryName))
            {
                try
                {
                    var category = new CategoryModel { Name = categoryName, Description = categoryDescription, UserId=user.Id};
                    _dataContext.Categories.Add(category);
                    await _dataContext.SaveChangesAsync();
                    TempData["Message"] = "Category created successfully!";
                }
                catch (DbUpdateException ex)
                {
                    TempData["Message"] = $"Error while creating category: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            // Redirect về action hiển thị danh sách danh mục
            return RedirectToAction("CreateCategory");
        }
 
        //Xóa danh mục sản phẩm
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var deleteCategory = await _dataContext.Categories.FindAsync(categoryId);
            if (deleteCategory != null)
            {
                _dataContext.Categories.Remove(deleteCategory);
                await _dataContext.SaveChangesAsync();
                TempData["Message"] = "Category deleted successfully!";
            }
            else
            {
                TempData["Message"] = "Category not found!";
            }
            return RedirectToAction("CreateCategory");
        }
        //Edit danh mục sản phẩm 
        public async Task<IActionResult> EditCategory(int editCategoryId)
        {
            //Tìm category cần edit dựa vào id
            var category = await _dataContext.Categories.FindAsync(editCategoryId);
            if (category == null)
            {
                //Trả về Index nếu không tìm thấy category cần edit
                ViewBag.Message = "Category not found.";
                return RedirectToAction("Index");
            }
            //Tìm thấy thì trả về view EditCategory với dữ liệu của category cần edit
            return View(category);
        }

        //Hàm xử lý edit 
        [HttpPost]
        public async Task<IActionResult> EditCategory(int editCategoryId, string editCategoryName, string editCategoryDescription)
        {
                var editCategory = await _dataContext.Categories.FindAsync(editCategoryId);
                if (editCategory != null)
                {
                    editCategory.Name = editCategoryName;
                    editCategory.Description = editCategoryDescription;

                    _dataContext.Categories.Update(editCategory);
                    await _dataContext.SaveChangesAsync();

                    TempData["Message"] = "Category updated successfully!";
                }
                else
                {
                    TempData["Message"] = "Category not found!";
                }

            return RedirectToAction("CreateCategory"); // Trả về trang danh sách danh mục sản phẩm
        }

        public async Task<IActionResult> AddQuantity(int Id)
        {
            var ProductByQuantity = await _dataContext.ProductQuantities.Where(pq => pq.ProductId == Id).ToListAsync();
            ViewBag.ProductByQuantity = ProductByQuantity;
            ViewBag.Id = Id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StoreProductQuantity(ProductQuantityModel productQuantityModel)
        {
            var product = _dataContext.Products.Find(productQuantityModel.ProductId);

            if (product == null)
            {
                return NotFound();
            }

            product.Quantity += productQuantityModel.Quantity;

            productQuantityModel.Quantity = productQuantityModel.Quantity;
            productQuantityModel.DateCreated = DateTime.Now;

            _dataContext.Add(productQuantityModel);
            _dataContext.SaveChangesAsync();
            TempData["success"] = "Thêm số lượng  sản phẩm thành công";
            return RedirectToAction("AddQuantity", "Seller", new { Id = productQuantityModel.ProductId });

        }


        public async Task<IActionResult> ManagerOrder()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _dataContext.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.Id).ToListAsync();

            return View(orders);

        }

        public async Task<IActionResult> ViewOrder(string codeorder, string storename)
        {
            // Sử dụng codeorder và storename theo nhu cầu của bạn
            var detailsOrder = await _dataContext.OrderDetails
                .Include(p => p.Products)
                .Where(p => p.OrderCode == codeorder && p.StoreName == storename) // Lọc theo StoreName
                .ToListAsync();

            var shippingCost = await _dataContext.Orders
                .Where(o => o.OrderCode == codeorder)
                .Select(o => o.ShippingCost)
                .FirstOrDefaultAsync();

            ViewBag.ShippingCost = shippingCost;

            // Nếu không tìm thấy đơn hàng, có thể trả về 404 hoặc một thông báo thích hợp
            if (detailsOrder == null || !detailsOrder.Any())
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            return View(detailsOrder);
        }




        [HttpPost]
        public async Task<IActionResult> UpdateOrders(string ordercode, int status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _dataContext.Orders.Where(o => o.UserId == userId).FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            if (order == null)
            {
                return NotFound();
            }


            order.Status = status;

            try
            {
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { success = false, message = ex.Message });
            }

        }

        //Thêm thương hiệu sản phẩm
        public async Task<IActionResult> CreateBrand()
        {
            var user = await _userManager.GetUserAsync(User);
            //Lấy danh sách thương hiệu sản phẩm để truyền vào CreateBrand
            var brands = _dataContext.Brands.Where(c => c.UserId == user.Id).ToList() ?? new List<BrandModel>(); // Lấy danh sách thương hiệu từ cơ sở dữ liệu
            return View(brands); // Truyền danh sách vào view
        }

        [HttpPost]
        public async Task<IActionResult> CreateBrand(string brandName, string brandDescription)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!string.IsNullOrEmpty(brandName))
            {
                try
                {
                    var brand = new BrandModel{ Name = brandName, Description = brandDescription, UserId=user.Id };
                    _dataContext.Brands.Add(brand);
                    await _dataContext.SaveChangesAsync();
                    TempData["Message"] = "Brand created successfully!";
                }
                catch (DbUpdateException ex)
                {
                    TempData["Message"] = $"Error while creating brand: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            // Redirect về action hiển thị danh sách brand
            return RedirectToAction("CreateBrand");
        }

        //Xóa brand
        [HttpPost]
        public async Task<IActionResult> DeleteBrand(int brandId)
        {
            var deleteBrand = await _dataContext.Brands.FindAsync(brandId);
            if (deleteBrand != null)
            {
                _dataContext.Brands.Remove(deleteBrand);
                await _dataContext.SaveChangesAsync();
                TempData["Message"] = "Brand deleted successfully!";
            }
            else
            {
                TempData["Message"] = "Brand not found!";
            }
            return RedirectToAction("CreateBrand");
        }
        //Edit brand
        public async Task<IActionResult> EditBrand(int editBrandId)
        {
            //Tìm brand cần edit dựa vào id
            var brand = await _dataContext.Brands.FindAsync(editBrandId);
            if (brand == null)
            {
                //Trả về Index nếu không tìm thấy brand cần edit
                ViewBag.Message = "Brand not found.";
                return RedirectToAction("Index");
            }
            //Tìm thấy thì trả về view EditBrand với dữ liệu của brand cần edit
            return View("EditBrand",brand);
        }

        [HttpPost]
        public async Task<IActionResult> EditBrand(int editBrandId, string editBrandName, string editBrandDescription)
        {
            var editBrand = await _dataContext.Brands.FindAsync(editBrandId);
            if (editBrand != null)
            {
                editBrand.Name = editBrandName;
                editBrand.Description = editBrandDescription;

                _dataContext.Brands.Update(editBrand);
                await _dataContext.SaveChangesAsync();

                TempData["Message"] = "Brand updated successfully!";
            }
            else
            {
                TempData["Message"] = "Brand not found!";
            }

            return RedirectToAction("CreateBrand"); // Trả về trang danh sách danh mục sản phẩm
        }



        public IActionResult RegisterSupplier()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterSupplier(SupplierDto supplierDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra xem người dùng đã là nhà cung cấp chưa
            var existingSupplier = await _dataContext.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (existingSupplier != null)
            {
                // Đã tồn tại, có thể hiển thị thông báo hoặc xử lý khác
                return BadRequest("Người dùng đã là nhà cung cấp.");
            }

            // Tạo mới nhà cung cấp
            var supplier = new Supplier
            {
                UserId = userId,
                StoreName = supplierDto.StoreName,
                Address = supplierDto.Address
            };

            _dataContext.Suppliers.Add(supplier);
            await _dataContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }


    }
}


