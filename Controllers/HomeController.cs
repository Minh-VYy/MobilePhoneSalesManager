using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly QuangCaoXmlService _quangCaoService;

    public HomeController(ILogger<HomeController> logger, QuangCaoXmlService quangCaoService)
    {
        _logger = logger;
        _quangCaoService = quangCaoService;
    }

    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("Role") == "Admin")
            return RedirectToAction("Index", "ThongKe");
        return View(_quangCaoService.GetActive());
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}