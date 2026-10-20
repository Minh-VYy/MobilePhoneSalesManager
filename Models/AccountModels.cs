using System.ComponentModel.DataAnnotations;

namespace QuanLyBanDienThoai.Models;

public class LoginInput
{
    [Required(ErrorMessage = "Nhập tên đăng nhập hoặc email."), StringLength(254)]
    public string Username { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(128)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class ProfileInput
{
    [Required(ErrorMessage = "Nhập họ và tên."), StringLength(100)]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập email."), EmailAddress(ErrorMessage = "Email chưa đúng định dạng."), StringLength(254)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại."), RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Nhập số điện thoại Việt Nam gồm 10 số.")]
    public string Phone { get; set; } = "";
    [StringLength(250)]
    public string? Address { get; set; } = "";
}

public class RegisterInput : ProfileInput
{
    [Required(ErrorMessage = "Nhập tên đăng nhập."), RegularExpression(@"^[a-zA-Z0-9_.-]{3,32}$", ErrorMessage = "Tên đăng nhập gồm 3–32 chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    public string Username { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(128, MinimumLength = 10, ErrorMessage = "Mật khẩu cần từ 10 đến 128 ký tự.")]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Nhập lại mật khẩu."), StringLength(128), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")]
    public string ConfirmPassword { get; set; } = "";
}

public class AdminSetupInput : RegisterInput
{
    [Required(ErrorMessage = "Nhập mã khởi tạo quản trị viên."), StringLength(64)]
    public string SetupKey { get; set; } = "";
}

public class ChangePasswordInput
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại."), StringLength(128)] public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu mới."), StringLength(128, MinimumLength = 10, ErrorMessage = "Mật khẩu mới cần từ 10 đến 128 ký tự.")]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Nhập lại mật khẩu mới."), StringLength(128), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class AccountRecord : ProfileInput
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string Role { get; set; } = "User";
    public string PasswordHash { get; set; } = "";
    public string SecurityStamp { get; set; } = "";
}
