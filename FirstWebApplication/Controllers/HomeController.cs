using FirstWebApplication.Entities;
using FirstWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FirstWebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _signInManager = signInManager;
        }

        // Viser forsiden med innloggings-/registreringsskjema
        // Redirecter innloggede brukere til riktig side
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Redirect authenticated users to their role-specific dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Pilot"))
                {
                    return RedirectToAction("RegisterType", "Pilot");
                }

                if (User.IsInRole("Registerfører"))
                {
                    return RedirectToAction("RegisterforerDashboard", "Registerforer");
                }
            }

            // Vis registreringsform hvis registrering feilet
            if (TempData["ShowRegister"] != null)
            {
                ViewBag.ShowRegister = true;

                if (TempData["RegisterErrors"] != null)
                {
                    var errors = TempData["RegisterErrors"]?.ToString()?.Split('|');
                    if (errors != null)
                    {
                        foreach (var error in errors)
                        {
                            if (!string.IsNullOrEmpty(error))
                                ModelState.AddModelError(string.Empty, error);
                        }
                    }
                }
            }
            // Vis innloggingsfeil hvis innlogging feilet
            else if (TempData["LoginErrors"] != null)
            {
                var errors = TempData["LoginErrors"]?.ToString()?.Split('|');
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        if (!string.IsNullOrEmpty(error))
                            ModelState.AddModelError(string.Empty, error);
                    }
                }
            }

            return View();
        }

        // Viser personvernside
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // Viser feilside med request ID for debugging
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
