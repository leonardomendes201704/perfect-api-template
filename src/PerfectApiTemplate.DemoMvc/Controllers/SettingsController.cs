using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PerfectApiTemplate.DemoMvc.Infrastructure;
using PerfectApiTemplate.DemoMvc.ViewModels.Settings;

namespace PerfectApiTemplate.DemoMvc.Controllers;

public sealed class SettingsController : Controller
{
    private readonly DemoOptions _options;

    public SettingsController(IOptions<DemoOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var current = HttpContext.Session.GetStringValue(SessionKeys.ApiBaseUrl) ?? _options.ApiBaseUrl;
        return View(new SettingsViewModel { ApiBaseUrl = current });
    }

    [HttpPost]
    public IActionResult Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        HttpContext.Session.SetStringValue(SessionKeys.ApiBaseUrl, model.ApiBaseUrl.Trim());
        TempData["Success"] = "API base URL updated.";
        return RedirectToAction(nameof(Index));
    }
}
