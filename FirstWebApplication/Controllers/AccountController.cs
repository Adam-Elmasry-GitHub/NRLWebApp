using FirstWebApplication.Entities;
using FirstWebApplication.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FirstWebApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ============================================================
        // REGISTER - Opprett ny bruker
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign role - default to "Pilot" if not specified
                    string role = string.IsNullOrEmpty(model.Role) ? "Pilot" : model.Role;
                    await _userManager.AddToRoleAsync(user, role);

                    // Logg inn brukeren
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect based on role with newUser parameter for task tracker
                    if (role == "Registerfører")
                    {
                        return RedirectToAction("RegisterforerDashboard", "Registerforer", new { newUser = "true" });
                    }
                    else
                    {
                        return RedirectToAction("RegisterType", "Pilot", new { newUser = "true" });
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            TempData["ShowRegister"] = true;
            TempData["RegisterErrors"] = string.Join("|", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return RedirectToAction("Index", "Home");
        }


        // LOGIN - Logg inn bruker

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    // Hent brukeren
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        // Hent brukerens roller
                        var roles = await _userManager.GetRolesAsync(user);

                        // Redirect basert på rolle (prioritering: Admin > Registerfører > Pilot)
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("AdminDashboard", "Admin");
                        }
                        else if (roles.Contains("Registerfører"))
                        {
                            return RedirectToAction("RegisterforerDashboard", "Registerforer");
                        }
                        else if (roles.Contains("Pilot"))
                        {
                            return RedirectToAction("RegisterType", "Pilot");
                        }
                        else
                        {
                            // Hvis ingen rolle (burde ikke skje), send til home
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            TempData["ShowLogin"] = true;
            TempData["LoginErrors"] = string.Join("|", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // LOGOUT - Logg ut bruker
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
