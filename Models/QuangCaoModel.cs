using System.ComponentModel.DataAnnotations;

namespace QuanLyBanDienThoai.Models;

public class QuangCaoModel
{
    public string MaQC { get; set; } = Guid.NewGuid().ToString("N");

    [Required(ErrorMessage = "Nhập tiêu đề banner."), StringLength(90, ErrorMessage = "Tiêu đề tối đa 90 ký tự.")]
    public string TieuDe { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập nội dung quảng cáo."), StringLength(320, ErrorMessage = "Mô tả tối đa 320 ký tự.")]
    public string MoTa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập đường dẫn hình ảnh."), StringLength(500)]
    [RegularExpression(@"^(?:https?://|/(?!/)|~/).+", ErrorMessage = "Dùng đường dẫn /images/… hoặc URL http/https.")]
    public string HinhAnh { get; set; } = string.Empty;

    [Range(1, 999)]
    public int ThuTu { get; set; } = 1;

    [Range(3, 30)]
    public int ThoiGianChuyenGiay { get; set; } = 6;

    public bool DangHienThi { get; set; } = true;
}
