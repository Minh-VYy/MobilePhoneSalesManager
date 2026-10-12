using System.ComponentModel.DataAnnotations;

namespace QuanLyBanDienThoai.Models;

public class QuangCaoModel
{
    public string MaQC { get; set; } = Guid.NewGuid().ToString("N");

    [Required, StringLength(90)]
    public string TieuDe { get; set; } = string.Empty;

    [Required, StringLength(320)]
    public string MoTa { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string HinhAnh { get; set; } = string.Empty;

    [Range(1, 999)]
    public int ThuTu { get; set; } = 1;

    [Range(3, 30)]
    public int ThoiGianChuyenGiay { get; set; } = 6;

    public bool DangHienThi { get; set; } = true;
}