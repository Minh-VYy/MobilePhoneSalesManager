using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CustomersController(AccountXmlService accounts, CustomerOrderService orders, SupportChatService chat) : Controller
{
    public IActionResult Index()
    {
        var byCustomer = orders.GetAll().ToLookup(x => x.AccountId);
        return View(accounts.Customers().Select(x => Summarize(x, byCustomer[x.Id].ToList())).ToList());
    }
    public IActionResult Details(string id)
    {
        var customer = accounts.Customers().FirstOrDefault(x => x.Id == id);
        if (customer == null) return NotFound();
        var history = orders.GetAll(id);
        return View(new CustomerDetailsModel(Summarize(customer, history), history, chat.Exists(id)));
    }
    private static CustomerSummary Summarize(AdminCustomer customer, List<BuyerOrder> history) => new(customer, history.Count,
        history.Count(x => x.Status is "Pending" or "Confirmed" or "Shipping"), history.Where(x => x.Status == "Delivered").Sum(x => x.Total),
        history.Count == 0 ? null : history.Max(x => x.CreatedAt));
}
