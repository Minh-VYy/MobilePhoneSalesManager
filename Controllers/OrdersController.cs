using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OrdersController(CustomerOrderService orders) : Controller
{
    private string AccountId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [Authorize(Roles = "User")]
    public IActionResult Index() => View(orders.GetAll(AccountId));
    [Authorize(Roles = "Admin")]
    public IActionResult Manage() => View("Index", orders.GetAll());
    public IActionResult Details(string id)
    {
        var order = orders.Get(id);
        if (order == null || (!User.IsInRole("Admin") && order.AccountId != AccountId)) return NotFound();
        return View(order);
    }
    [Authorize(Roles = "User"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(CustomerOrderRequestModel input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var order = orders.Create(AccountId, input);
                TempData["OrderCreated"] = order.Id;
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View("~/Views/DienThoai/Cart.cshtml", input);
    }
    [Authorize(Roles = "User"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(string id)
    {
        try { orders.ChangeStatus(id, "Cancelled", AccountId); TempData["SuccessMessage"] = "Đã hủy đơn hàng."; }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(string id, string status)
    {
        try { orders.ChangeStatus(id, status); TempData["SuccessMessage"] = "Đã cập nhật đơn hàng."; }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
