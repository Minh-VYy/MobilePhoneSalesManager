using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class StockUnitsController(StockUnitService units, DienThoaiXmlService phones) : Controller
{
    public IActionResult Index(string? productId = null) => View(Page(new RegisterStockUnitInput { ProductId = productId ?? "" }));
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Register([Bind(Prefix = "Input")] RegisterStockUnitInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                units.Register(input, User.FindFirst("DisplayName")?.Value ?? User.Identity!.Name!);
                TempData["SuccessMessage"] = $"Đã gắn mã {input.Code.Trim()} cho máy trong kho.";
                return RedirectToAction(nameof(Index), new { productId = input.ProductId });
            }
            catch (InvalidOperationException error) { ModelState.AddModelError("", error.Message); }
        }
        return View("Index", Page(input));
    }
    [HttpGet]
    public IActionResult Lookup(string code)
    {
        try
        {
            var unit = units.Lookup(code);
            if (unit == null) return NotFound(new { error = "Chưa đăng ký mã máy này. Hãy gắn IMEI/serial với phiên bản trong mục Mã máy & IMEI trước khi bán." });
            var phone = phones.GetById(unit.ProductId);
            return Json(new
            {
                unit.Code,
                unit.ProductId,
                name = phone?.TenSP ?? unit.ProductName,
                price = phone?.GiaBan ?? 0,
                stock = phone?.SoLuongTon ?? 0,
                unit.Status,
                unit.StatusLabel,
                unit.Reference,
                unit.Channel,
                unit.SoldAt,
                unit.SalePrice,
                productExists = phone != null,
                referenceUrl = string.IsNullOrEmpty(unit.Reference) ? null : Url.Action(unit.Channel == "POS" ? "Print" : "Details", unit.Channel == "POS" ? "HoaDon" : "Orders", new { id = unit.Reference })
            });
        }
        catch (InvalidOperationException error) { return BadRequest(new { error = error.Message }); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Unregister(string code)
    {
        try { units.Unregister(code); TempData["SuccessMessage"] = "Đã gỡ mã đăng ký. Số lượng tồn kho không thay đổi."; }
        catch (InvalidOperationException error) { TempData["ErrorMessage"] = error.Message; }
        return RedirectToAction(nameof(Index));
    }
    private StockUnitPageModel Page(RegisterStockUnitInput input) => new() { Input = input, Products = phones.GetAll(), Units = units.GetAll() };
}
