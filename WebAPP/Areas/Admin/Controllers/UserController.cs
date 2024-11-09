using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAPP.Models;
using WebAPP.Models.Repository;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace WebAPP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/User")]
    [Authorize(Roles ="Admin")]
    public class UserController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly DataContext _dataContext;

        public UserController(RoleManager<IdentityRole> roleManager, UserManager<AppUserModel> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách người dùng
            var users = await _userManager.Users
                                           .OrderByDescending(u => u.Id)
                                           .ToListAsync();

            // Tạo một danh sách để chứa thông tin người dùng với vai trò
            var userList = new List<AppUserModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new AppUserModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    RoleId = roles.FirstOrDefault(), // Giả sử chỉ lấy một vai trò đầu tiên, nếu cần lấy nhiều vai trò thì có thể điều chỉnh
                });
            }

            return View(userList); // Trả về danh sách người dùng có vai trò
        }


        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");

            return View(new AppUserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(AppUserModel user, string password)
        {
            if (ModelState.IsValid)
            {
                var createUserResult = await _userManager.CreateAsync(user, user.PasswordHash); if (createUserResult.Succeeded)
                {
                    if (!string.IsNullOrEmpty(user.RoleId))
                    {
                        await _userManager.AddToRoleAsync(user, user.RoleId);
                    }
                    return RedirectToAction("Index");
                }
                else
                {
                    AddIdentityErrors(createUserResult);
                }
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");
            return View(user);
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return View("Error");
            }

            TempData["success"] = "User deleted successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id, AppUserModel user, string password)
        {
            var existingUser = await _userManager.FindByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;

                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (updateResult.Succeeded)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                        var passwordResult = await _userManager.ResetPasswordAsync(existingUser, token, password);
                        if (!passwordResult.Succeeded)
                        {
                            AddIdentityErrors(passwordResult);
                            var roles = await _roleManager.Roles.ToListAsync();
                            ViewBag.Roles = new SelectList(roles, "Name", "Name");
                            return View(existingUser);
                        }
                    }

                    // Cập nhật vai trò
                    if (!string.IsNullOrEmpty(user.RoleId))
                    {
                        var currentRoles = await _userManager.GetRolesAsync(existingUser);
                        await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);
                        await _userManager.AddToRoleAsync(existingUser, user.RoleId);
                    }


                    return RedirectToAction("Index");
                }
                else
                {
                    AddIdentityErrors(updateResult);
                }
            }

            var allRoles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(allRoles, "Name", "Name");
            return View(existingUser);
        }

    }
}
