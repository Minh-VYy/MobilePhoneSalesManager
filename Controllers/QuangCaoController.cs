using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuangCaoController : Controller
{
    private readonly QuangCaoXmlService _service;
    public QuangCaoController(QuangCaoXmlService service) => _service = service;

    public IActionResult Index()
    {
        return View(_service.GetAll());
    }

    [HttpGet]
    public IActionResult Create() => Editor(new QuangCaoModel { ThuTu = Math.Min(999, (_service.GetAll().Select(x => x.ThuTu).DefaultIfEmpty(0).Max()) + 1) }, false);

    [HttpGet]
    public IActionResult Edit(string id)
    {
        var ad = _service.GetAll().FirstOrDefault(x => x.MaQC == id);
        return ad == null ? NotFound() : Editor(ad, true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(QuangCaoModel model)
    {
        if (!ModelState.IsValid) return Editor(model, false);
        _service.Add(model);
        TempData["SuccessMessage"] = "Đã thêm banner quảng cáo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Save(QuangCaoModel model)
    {
        if (!ModelState.IsValid) return Editor(model, true);
        if (!_service.Update(model)) return NotFound();
        TempData["SuccessMessage"] = "Đã cập nhật banner quảng cáo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(string id)
    {
        if (_service.Delete(id)) TempData["SuccessMessage"] = "Đã xóa banner quảng cáo.";
        else TempData["ErrorMessage"] = "Banner không còn tồn tại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Toggle(string id, bool enabled)
    {
        if (!_service.SetVisibility(id, enabled)) return NotFound();
        TempData["SuccessMessage"] = enabled ? "Đã bật hiển thị banner." : "Đã tạm ẩn banner.";
        return RedirectToAction(nameof(Index));
    }

    private ViewResult Editor(QuangCaoModel model, bool editing)
    {
        ViewData["EditingBanner"] = editing;
        ViewData["Title"] = editing ? "Chỉnh sửa banner" : "Thêm banner";
        return View("Form", model);
    }
}
