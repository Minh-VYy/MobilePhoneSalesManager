using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly QuangCaoXmlService _quangCaoService;
    private readonly DienThoaiXmlService _dienThoaiService;
    private readonly TinTucXmlService _tinTucService;

    public HomeController(
        ILogger<HomeController> logger,
        QuangCaoXmlService quangCaoService,
        DienThoaiXmlService dienThoaiService,
        TinTucXmlService tinTucService)
    {
        _logger = logger;
        _quangCaoService = quangCaoService;
        _dienThoaiService = dienThoaiService;
        _tinTucService = tinTucService;
    }

    public IActionResult Index()
    {
        var allPhones = _dienThoaiService.GetAll();
        var apple = allPhones.Where(p => string.Equals(p.MaHang, "APPLE", StringComparison.OrdinalIgnoreCase) && !p.MaSP.StartsWith("ACC-")).ToList();
        var android = allPhones.Where(p => !string.Equals(p.MaHang, "APPLE", StringComparison.OrdinalIgnoreCase) && !p.MaSP.StartsWith("ACC-")).ToList();
        var accessories = allPhones.Where(p => p.MaSP.StartsWith("ACC-")).ToList();

        // Flash sale: selection of prominent flagships with discounted prices
        var flashSale = allPhones.Where(p => p.GiaBan > 15000000 || p.MaSP.StartsWith("ACC-")).Take(8).ToList();

        var viewModel = new HomeIndexViewModel
        {
            QuangCao = _quangCaoService.GetActive(),
            FlashSale = flashSale,
            ApplePhones = apple,
            AndroidPhones = android,
            Phukien = accessories,
            TinTuc = _tinTucService.GetLatest(6)
        };

        return View(viewModel);
    }

    public IActionResult Privacy() => View();
    public IActionResult Policies() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
