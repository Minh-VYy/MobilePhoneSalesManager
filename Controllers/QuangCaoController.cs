using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuangCaoController : Controller
{
    private readonly QuangCaoXmlService _service;
    private readonly VoucherService _vouchers;
    public QuangCaoController(QuangCaoXmlService service, VoucherService vouchers) { _service = service; _vouchers = vouchers; }

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
        ValidatePromotion(model);
        if (!ModelState.IsValid) return Editor(model, false);
        _service.Add(model);
        TempData["SuccessMessage"] = "Đã thêm banner quảng cáo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Save(QuangCaoModel model)
    {
        ValidatePromotion(model);
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

    private void ValidatePromotion(QuangCaoModel model)
    {
        if (string.IsNullOrWhiteSpace(model.PromotionCode)) { model.PromotionCode = ""; return; }
        try
        {
            model.PromotionCode = VoucherService.Normalize(model.PromotionCode);
            if (_vouchers.Get(model.PromotionCode) is not { Public: true }) ModelState.AddModelError(nameof(model.PromotionCode), "Chọn một thẻ giảm giá công khai đang có trên hệ thống.");
        }
        catch (InvalidOperationException error) { ModelState.AddModelError(nameof(model.PromotionCode), error.Message); }
    }
    private ViewResult Editor(QuangCaoModel model, bool editing)
    {
        ViewData["EditingBanner"] = editing;
        ViewData["Title"] = editing ? "Chỉnh sửa banner" : "Thêm banner";
        return View("Form", model);
    }
}
