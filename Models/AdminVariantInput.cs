using System.ComponentModel.DataAnnotations;

namespace QuanLyBanDienThoai.Models;

public sealed class AdminVariantInput
{
    [Required] public string ProductId { get; set; } = "";
    [Required(ErrorMessage = "Nhập tên dòng máy để gộp các phiên bản."), StringLength(100)] public string ModelSeries { get; set; } = "";
    [Required, RegularExpression("New|Used|Refurbished")] public string ConditionType { get; set; } = "New";
    [Required(ErrorMessage = "Nhập mức tình trạng máy."), StringLength(50)] public string ConditionGrade { get; set; } = "100%";
    [StringLength(500)] public string? ConditionDescription { get; set; }
    [Range(0, 60)] public int WarrantyMonths { get; set; }
    [Required(ErrorMessage = "Nhập tên màu sắc."), StringLength(60)] public string ColorName { get; set; } = "";
    [Required, RegularExpression("#[0-9a-fA-F]{6}", ErrorMessage = "Màu cần đúng mã hex, ví dụ #9ca3af.")] public string ColorCode { get; set; } = "#9ca3af";
    [Range(1, 4096, ErrorMessage = "Dung lượng cần từ 1 đến 4096 GB.")] public int StorageGB { get; set; } = 128;
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal OriginalPrice { get; set; }
    [StringLength(100)] public string? Sku { get; set; }
    [StringLength(500), RegularExpression(@"^(?:https?://|/(?!/)|~/).+", ErrorMessage = "Dùng đường dẫn /images/… hoặc URL http/https.")] public string? ImageUrl { get; set; }
    [StringLength(250)] public string? Camera { get; set; }
    [StringLength(100)] public string? Resolution { get; set; }
    [StringLength(150)] public string? ChargingTechnology { get; set; }
    [StringLength(100)] public string? Ports { get; set; }
    [StringLength(50)] public string? Weight { get; set; }
}
