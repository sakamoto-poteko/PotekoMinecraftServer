using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PotekoMinecraftServer.Models;
using PotekoMinecraftServer.Services;

namespace PotekoMinecraftServer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMinecraftMonitorService _monitorService;

        public HomeController(ILogger<HomeController> logger, IMinecraftMonitorService monitorService)
        {
            _logger = logger;
            _monitorService = monitorService;
        }

        public IActionResult Index()
        {
            return View(_monitorService.ListServerNames());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
