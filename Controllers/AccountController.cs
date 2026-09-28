using Microsoft.AspNetCore.Mvc;

namespace QuanLyBanDienThoai.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password, string role = "User", string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!";
                return View();
            }

            // Save User Session & Role
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("Role", role);
            HttpContext.Session.SetString("IsLoggedIn", "true");

            TempData["SuccessMessage"] = $"Đăng nhập thành công với vai trò {(role == "Admin" ? "Quản Trị Viên" : "Khách Hàng")}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (role == "Admin")
            {
                return RedirectToAction("Index", "ThongKe");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string fullname, string email, string phone, string username, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            // Save Registered User
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("Role", "User");
            HttpContext.Session.SetString("IsLoggedIn", "true");

            TempData["SuccessMessage"] = "Đăng ký tài khoản mới thành công! Chào mừng bạn đến với PhoneStore 3D.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/SwitchRole
        public IActionResult SwitchRole(string role, string? returnUrl = null)
        {
            var targetRole = role == "Admin" ? "Admin" : "User";
            HttpContext.Session.SetString("Role", targetRole);
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
            {
                HttpContext.Session.SetString("Username", targetRole == "Admin" ? "AdminDemo" : "KhachHangDemo");
                HttpContext.Session.SetString("IsLoggedIn", "true");
            }

            TempData["SuccessMessage"] = $"Đã chuyển giao diện sang chế độ: {(targetRole == "Admin" ? "Giao Diện Quản Trị (Admin Dashboard)" : "Giao Diện Khách Hàng (User Storefront)")}";

            if (targetRole == "Admin")
            {
                return RedirectToAction("Index", "ThongKe");
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất khỏi hệ thống!";
            return RedirectToAction("Index", "Home");
        }
    }
}
