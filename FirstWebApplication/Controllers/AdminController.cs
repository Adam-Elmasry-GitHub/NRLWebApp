using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FirstWebApplication.Services;

namespace FirstWebApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserRoleService _roleService;
        private readonly ApplicationDbContext _context;
        private readonly ExportService _exportService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            UserRoleService roleService,
            ApplicationDbContext context,
            ExportService exportService)
        {
            _userManager = userManager;
            _roleService = roleService;
            _context = context;
            _exportService = exportService;
        }

        // Viser admin dashboard med oversikt over brukere og hindringer
        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            var statistics = await GetDashboardStatisticsAsync();

            ViewBag.TotalUsers = statistics.TotalUsers;
            ViewBag.TotalObstacles = statistics.TotalObstacles;
            ViewBag.ApprovedObstacles = statistics.ApprovedObstacles;
            ViewBag.PendingObstacles = statistics.PendingObstacles;
            ViewBag.RejectedObstacles = statistics.RejectedObstacles;
            ViewBag.PilotCount = statistics.PilotCount;
            ViewBag.RegisterforerCount = statistics.RegisterforerCount;
            ViewBag.AdminCount = statistics.AdminCount;

            return View();
        }

        // Viser liste over alle brukere med mulighet for rollefiltrering
        [HttpGet]
        public async Task<IActionResult> AdminUsers(string roleFilter = "all")
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = await GetUserRolesAsync(users);

            // Filtrer brukere basert på rolle
            if (roleFilter != "all")
            {
                users = users
                    .Where(u => userRoles.ContainsKey(u.Id) && userRoles[u.Id].Contains(roleFilter))
                    .ToList();
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.CurrentFilter = roleFilter;

            return View(users);
        }

        // Viser side for å administrere en spesifikk brukers roller
        [HttpGet]
        public async Task<IActionResult> AdminManageUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleService.GetAllRolesAsync();

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = allRoles;

            return View(user);
        }

        // Tildeler en rolle til bruker og fjerner alle eksisterende roller
        // Implementerer EN rolle per bruker policy
        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Fjern alle eksisterende roller (EN rolle per bruker policy)
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                TempData["Message"] = $"Role '{roleName}' assigned to {user.Email} (previous roles removed)";
            }
            else
            {
                TempData["Error"] = "Failed to assign role";
            }

            return RedirectToAction("AdminManageUser", new { id = userId });
        }

        // Fjerner en rolle fra bruker
        // Beskytter mot fjerning av siste admin
        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Sjekk at vi ikke fjerner siste admin
            if (roleName == "Admin")
            {
                var admins = await _roleService.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "Cannot remove the last Admin user!";
                    return RedirectToAction("AdminManageUser", new { id = userId });
                }
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                TempData["Message"] = $"Role '{roleName}' removed from {user.Email}";
            }
            else
            {
                TempData["Error"] = "Failed to remove role";
            }

            return RedirectToAction("AdminManageUser", new { id = userId });
        }

        // Sletter en bruker fra systemet
        // Beskytter mot å slette seg selv og siste admin
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Kan ikke slette seg selv
            if (user.Email == User.Identity?.Name)
            {
                TempData["Error"] = "You cannot delete your own account!";
                return RedirectToAction("AdminUsers");
            }

            // Sjekk at vi ikke sletter siste admin
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                var admins = await _roleService.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "Cannot delete the last Admin user!";
                    return RedirectToAction("AdminUsers");
                }
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["Message"] = $"User {user.Email} has been deleted";
            }
            else
            {
                TempData["Error"] = "Failed to delete user";
            }

            return RedirectToAction("AdminUsers");
        }

        // Viser detaljert statistikkside med hindring- og brukerdata
        [HttpGet]
        public async Task<IActionResult> AdminStatistics()
        {
            var oneWeekAgo = DateTime.Now.AddDays(-7);

            // Hent hindring-statistikk
            var obstacleStats = new
            {
                Total = await _context.Obstacles.CountAsync(),
                Approved = await GetObstacleCountByStatusAsync(3),
                Pending = await GetObstacleCountByStatusAsync(2),
                Rejected = await GetObstacleCountByStatusAsync(4),
                ThisWeek = await _context.Obstacles
                    .Where(o => o.RegisteredDate >= oneWeekAgo)
                    .CountAsync()
            };

            // Hent brukerstatistikk
            var pilots = await _roleService.GetUsersInRoleAsync("Pilot");
            var registerforers = await _roleService.GetUsersInRoleAsync("Registerfører");
            var admins = await _roleService.GetUsersInRoleAsync("Admin");

            ViewBag.ObstacleStats = obstacleStats;
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.PilotCount = pilots.Count;
            ViewBag.RegisterforerCount = registerforers.Count;
            ViewBag.AdminCount = admins.Count;

            return View();
        }

        // Eksporterer alle hindringer til Excel-fil med formatering
        [HttpGet]
        public async Task<IActionResult> ExportObstaclesToExcel()
        {
            var obstacles = await _context.Obstacles
                .Include(o => o.RegisteredByUser)
                .Include(o => o.ObstacleType)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s!.StatusType)
                .OrderByDescending(o => o.RegisteredDate)
                .ToListAsync();

            var content = _exportService.GenerateObstaclesExcel(obstacles);
            var fileName = $"Obstacles_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Hjelpe-metoder

        // Henter samlet statistikk for dashboard
        private async Task<DashboardStatistics> GetDashboardStatisticsAsync()
        {
            var pilots = await _roleService.GetUsersInRoleAsync("Pilot");
            var registerforers = await _roleService.GetUsersInRoleAsync("Registerfører");
            var admins = await _roleService.GetUsersInRoleAsync("Admin");

            return new DashboardStatistics
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalObstacles = await _context.Obstacles.CountAsync(),
                ApprovedObstacles = await GetObstacleCountByStatusAsync(3),
                PendingObstacles = await GetObstacleCountByStatusAsync(2),
                RejectedObstacles = await GetObstacleCountByStatusAsync(4),
                PilotCount = pilots.Count,
                RegisterforerCount = registerforers.Count,
                AdminCount = admins.Count
            };
        }

        // Teller antall hindringer med gitt status
        private async Task<int> GetObstacleCountByStatusAsync(int statusTypeId)
        {
            return await _context.ObstacleStatuses
                .Where(s => s.IsActive && s.StatusTypeId == statusTypeId)
                .Select(s => s.ObstacleId)
                .Distinct()
                .CountAsync();
        }

        // Henter roller for en liste med brukere
        private async Task<Dictionary<string, IList<string>>> GetUserRolesAsync(List<ApplicationUser> users)
        {
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles;
            }
            return userRoles;
        }

        // (Excel helper logic moved to ExportService)

        // Hjelpeklasse for statistikk
        private class DashboardStatistics
        {
            public int TotalUsers { get; set; }
            public int TotalObstacles { get; set; }
            public int ApprovedObstacles { get; set; }
            public int PendingObstacles { get; set; }
            public int RejectedObstacles { get; set; }
            public int PilotCount { get; set; }
            public int RegisterforerCount { get; set; }
            public int AdminCount { get; set; }
        }
    }
}
