using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PotekoMinecraftServer.Models;

namespace PotekoMinecraftServer.Controllers
{
    public class ConfigController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ConfigController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private readonly ResultResponse _alreadyInitialized = new ResultResponse
        {
            Result = true,
            Message = "Initialization already performed"
        };

        [Authorize]
        public async Task<IActionResult> Initialize()
        {
            var hasRole = await _roleManager.RoleExistsAsync(UserRoles.Player);
            if (!hasRole)
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(UserRoles.Player));
                if (!result.Succeeded)
                {
                    return View(new ResultResponse
                    {
                        Result = false,
                        Message = string.Join('\n', result.Errors)
                    });
                }
            }

            if (_userManager.Users.Count() == 1)
            {
                var user = _userManager.Users.Single();
                var addedToRole = await _userManager.IsInRoleAsync(user, UserRoles.Player);
                if (addedToRole)
                {
                    return View(_alreadyInitialized);
                }
                else
                {
                    var result = await _userManager.AddToRoleAsync(user, UserRoles.Player);
                    return View(new ResultResponse
                    {
                        Result = result.Succeeded,
                        Message = result.Succeeded ? "Initialization success" : string.Join('\n', result.Errors)
                    });
                }
            }
            else
            {
                return View(_alreadyInitialized);
            }
        }

        [Authorize(Roles = UserRoles.Player)]
        public IActionResult ConfirmRole()
        {
            return View();
        }
    }
}