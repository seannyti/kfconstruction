using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionWeb.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class DiagnosticsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public DiagnosticsController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add($"Email: {user.Email}, Username: {user.UserName}, Roles: {string.Join(", ", roles)}");
            }

            ViewBag.Users = userList;
            ViewBag.TotalUsers = users.Count;
            return View();
        }
    }
}
