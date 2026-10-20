using System.ComponentModel.DataAnnotations;

namespace QuanLyBanDienThoai.Models;

public sealed class RegisterStockUnitInput
{
    [Required(ErrorMessage = "Chọn phiên bản sản phẩm.")] public string ProductId { get; set; } = "";
    [Required(ErrorMessage = "Quét hoặc nhập IMEI/serial của máy."), RegularExpression(@"[A-Za-z0-9][A-Za-z0-9._/-]{2,79}", ErrorMessage = "Mã máy gồm 3–80 chữ, số hoặc ký tự . _ / -.")]
    public string Code { get; set; } = "";
    [RegularExpression(@"[A-Za-z0-9][A-Za-z0-9._/-]{2,79}", ErrorMessage = "Mã phụ gồm 3–80 chữ, số hoặc ký tự . _ / -.")]
    public string? AlternateCode { get; set; }
}

public sealed record StockUnit(string Code, string? AlternateCode, string ProductId, string ProductName, string Status,
    DateTimeOffset RegisteredAt, string Reference, string Channel, DateTimeOffset? SoldAt, decimal SalePrice, string Employee)
{
    public string StatusLabel => Status switch { "Available" => "Còn trong kho", "Reserved" => "Đang giữ cho đơn online", "Sold" => "Đã bán", _ => Status };
}
public sealed class StockUnitPageModel
{
    public RegisterStockUnitInput Input { get; set; } = new();
    public List<DienThoaiModel> Products { get; set; } = [];
    public List<StockUnit> Units { get; set; } = [];
}
