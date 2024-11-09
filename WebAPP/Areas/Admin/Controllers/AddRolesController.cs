using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAPP.Models;

namespace WebAPP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AddRolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUserModel> _userManager;

        public AddRolesController(RoleManager<IdentityRole> roleManager, UserManager<AppUserModel> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var roles = _roleManager.Roles?.ToList() ?? new List<IdentityRole>();
            return View("Index", roles); // Ensuring the roles list is passed correctly.
        }

        [HttpPost]
        public async Task<IActionResult> AddRole(string roleName)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        ViewBag.Message = "Role created successfully!";
                    }
                    else
                    {
                        ViewBag.Message = "Error while creating role.";
                    }
                }
                else
                {
                    ViewBag.Message = "Role already exists.";
                }
            }

            var roles = _roleManager.Roles?.ToList() ?? new List<IdentityRole>(); // Fetch updated roles
            return View("Index", roles); // Return updated view
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    ViewBag.Message = "Role deleted successfully.";
                }
                else
                {
                    ViewBag.Message = "Error deleting role.";
                }
            }
            else
            {
                ViewBag.Message = "Role not found.";
            }

            var roles = _roleManager.Roles.ToList();
            return View("Index", roles); // Return updated view with roles and message
        }


        public async Task<IActionResult> EditRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                ViewBag.Message = "Role not found.";
                return RedirectToAction("Index");
            }

            return View("EditRole", role);
        }

        // POST: Save Role after Edit
        [HttpPost]
        public async Task<IActionResult> EditRole(string roleId, string roleName)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role != null)
            {
                role.Name = roleName;
                var result = await _roleManager.UpdateAsync(role);
                if (result.Succeeded)
                {
                    ViewBag.Message = "Role updated successfully!";
                }
                else
                {
                    ViewBag.Message = "Error while updating role.";
                }
            }
            else
            {
                ViewBag.Message = "Role not found.";
            }

            var roles = _roleManager.Roles.ToList();
            return View("Index", roles); // Return to the main role management page
        }   
    }
}
