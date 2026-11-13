using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class VouchersController(VoucherService vouchers, DienThoaiXmlService phones) : Controller
{
    public IActionResult Index() => View(vouchers.GetAll());
    public IActionResult Create() => View("Form", Page(new VoucherInput(), false));
    public IActionResult Edit(string id)
    {
        var voucher = vouchers.Get(id);
        return voucher == null ? NotFound() : View("Form", Page(voucher, true));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Save([Bind(Prefix = "Input")] VoucherInput input, bool editing)
    {
        if (ModelState.IsValid)
        {
            try
            {
                vouchers.Save(input, !editing);
                TempData["SuccessMessage"] = editing ? "Đã cập nhật thẻ giảm giá." : "Đã tạo mã và thẻ giảm giá.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException error) { ModelState.AddModelError("", error.Message); }
        }
        return View("Form", Page(input, editing));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Toggle(string code, bool enabled)
    {
        try { vouchers.SetEnabled(code, enabled); TempData["SuccessMessage"] = enabled ? "Đã bật mã giảm giá." : "Đã tắt mã giảm giá."; }
        catch (InvalidOperationException error) { TempData["ErrorMessage"] = error.Message; }
        return RedirectToAction(nameof(Index));
    }
    private VoucherFormViewModel Page(VoucherInput input, bool editing) => new() { Input = input, Editing = editing, Products = phones.GetAll().OrderBy(x => x.TenSP).ToList() };
}
