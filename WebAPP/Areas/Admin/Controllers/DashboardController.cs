﻿using Microsoft.AspNetCore.Mvc;

namespace WebAPP.Areas.Admin.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
