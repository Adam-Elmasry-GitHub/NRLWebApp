using System;
using System.Security.Policy;
using FirstWebApplication.Models.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace FirstWebApplication.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            // Hent nåværende kultur fra requesten
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();

            var model = new SettingsViewModel
            {
                CurrentLanguage = requestCulture?.RequestCulture.UICulture.Name ?? "nb-NO",
                // Tema leses vanligvis fra localStorage i browser, 
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SetLanguage(SettingsViewModel model, string returnUrl = null)
        {
            // Sett språk-cookie som varer i 1 år
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(model.CurrentLanguage)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Gå tilbake til siden man kom fra, eller Settings-siden
            return LocalRedirect(returnUrl ?? Url.Action("Index"));
        }
    }
}