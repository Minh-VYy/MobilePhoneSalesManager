using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PromotionsController(VoucherService vouchers) : Controller
{
    public IActionResult Index() => View(vouchers.PublicCards());
}
