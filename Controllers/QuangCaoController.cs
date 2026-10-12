using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

public class QuangCaoController : Controller
{
    private readonly QuangCaoXmlService _service;
    public QuangCaoController(QuangCaoXmlService service) => _service = service;

    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
        return View(_service.GetAll());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(QuangCaoModel model)
    {
        if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Vui lòng kiểm tra tiêu đề, mô tả, ảnh và thời gian chuyển."; return RedirectToAction(nameof(Index)); }
        _service.Add(model);
        TempData["SuccessMessage"] = "Đã thêm banner quảng cáo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Save(QuangCaoModel model)
    {
        if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid || !_service.Update(model))
        {
            TempData["ErrorMessage"] = "Không thể lưu banner. Hãy kiểm tra lại dữ liệu.";
            return RedirectToAction(nameof(Index));
        }
        TempData["SuccessMessage"] = "Đã cập nhật banner quảng cáo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(string id)
    {
        if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
        _service.Delete(id);
        TempData["SuccessMessage"] = "Đã xóa banner quảng cáo.";
        return RedirectToAction(nameof(Index));
    }
}