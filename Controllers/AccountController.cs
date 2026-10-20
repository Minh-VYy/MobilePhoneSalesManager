using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountController(AccountXmlService accounts) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", User.IsInRole("Admin") ? "ThongKe" : "Home");
        ViewBag.HasAdmin = accounts.HasAdmin;
        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        ViewBag.HasAdmin = accounts.HasAdmin;
        if (!ModelState.IsValid) return View(input);
        var account = accounts.Authenticate(input.Username, input.Password);
        if (account == null)
        {
            ModelState.AddModelError("", "Tên đăng nhập/email hoặc mật khẩu chưa đúng. Nếu nhập sai nhiều lần, hãy thử lại sau 15 phút.");
            return View(input);
        }
        await SignIn(account);
        if (account.Role == "Admin") return RedirectToAction("Index", "ThongKe");
        if (Url.IsLocalUrl(input.ReturnUrl) && !input.ReturnUrl!.Contains("/Account/", StringComparison.OrdinalIgnoreCase)) return LocalRedirect(input.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet] public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(Profile)) : View(new RegisterInput());

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public IActionResult Register(RegisterInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var error = accounts.Register(input);
        if (error != null) { ModelState.AddModelError("", error); return View(input); }
        TempData["SuccessMessage"] = "Tài khoản đã được tạo. Hãy đăng nhập để bắt đầu mua sắm.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet] public IActionResult Setup() => accounts.HasAdmin ? NotFound() : View(new AdminSetupInput());

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public IActionResult Setup(AdminSetupInput input)
    {
        if (accounts.HasAdmin) return NotFound();
        if (!ModelState.IsValid) return View(input);
        var error = accounts.Register(input, true, input.SetupKey);
        if (error != null) { ModelState.AddModelError("", error); return View(input); }
        TempData["SuccessMessage"] = "Đã tạo quản trị viên. Đăng nhập bằng tài khoản vừa thiết lập.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpGet]
    public IActionResult Profile()
    {
        var account = accounts.Find(User.FindFirstValue(ClaimTypes.NameIdentifier))!;
        return View(new ProfileInput { FullName = account.FullName, Email = account.Email, Phone = account.Phone, Address = account.Address });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var error = accounts.UpdateProfile(id, input);
        if (error != null) { ModelState.AddModelError("", error); return View(input); }
        await SignIn(accounts.Find(id)!);
        TempData["SuccessMessage"] = "Đã lưu thông tin tài khoản.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpGet] public IActionResult ChangePassword() => View(new ChangePasswordInput());

    [Authorize, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        if (!ModelState.IsValid) return View(input);
        if (!accounts.ChangePassword(User.FindFirstValue(ClaimTypes.NameIdentifier)!, input.CurrentPassword, input.Password))
        { ModelState.AddModelError(nameof(input.CurrentPassword), "Mật khẩu hiện tại chưa đúng."); return View(input); }
        await HttpContext.SignOutAsync();
        TempData["SuccessMessage"] = "Đã đổi mật khẩu. Vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }

    private Task SignIn(AccountRecord account) => HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, account.Id), new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Role, account.Role), new Claim("DisplayName", account.FullName), new Claim("SecurityStamp", account.SecurityStamp)
        }, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
}
