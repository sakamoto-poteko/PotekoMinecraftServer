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
        private readonly IMinecraftServerDaemonService _minecraftServerDaemonService;
        private readonly IMinecraftMachineService _minecraftMachineService;


        public HomeController(ILogger<HomeController> logger, 
            IMinecraftMonitorService monitorService, 
            IMinecraftServerDaemonService minecraftServerDaemonService,
            IMinecraftMachineService minecraftMachineService)
        {
            _logger = logger;
            _monitorService = monitorService;
            _minecraftServerDaemonService = minecraftServerDaemonService;
            _minecraftMachineService = minecraftMachineService;
        }

        public IActionResult Index()
        {
            return View(_monitorService.ListServerNames());
        }

        private enum ServerOperations
        {
            Start,
            Stop
        }

        private async Task<IActionResult> ExecuteServerOperation(string name, ServerOperations op)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest();
            }

            var servers = _monitorService.ListServerNames();
            if (!servers.Contains(name))
            {
                return NotFound();
            }


            bool finished;
            switch (op)
            {
                case ServerOperations.Start:
                    finished = await _minecraftServerDaemonService.StartServerAsync(name);
                    break;
                case ServerOperations.Stop:
                    finished = await _minecraftServerDaemonService.StopServerAsync(name);
                    break;
                default:
                    return NotFound();
            }

            return Ok(new
            {
                Completed = finished
            });
        }

        [HttpPost]
        public async Task<IActionResult> StartMachine([FromQuery] string name)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _minecraftMachineService.StartAsync(name);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> StartServer([FromQuery] string name)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return await ExecuteServerOperation(name, ServerOperations.Start);
        }


        [HttpPost]
        public async Task<IActionResult> StopServer([FromQuery] string name)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return await ExecuteServerOperation(name, ServerOperations.Stop);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
