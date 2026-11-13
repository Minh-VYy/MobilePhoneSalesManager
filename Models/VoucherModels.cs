using System.ComponentModel.DataAnnotations;

namespace QuanLyBanDienThoai.Models;

public class VoucherInput
{
    [Required, RegularExpression(@"[A-Za-z0-9][A-Za-z0-9_-]{2,31}", ErrorMessage = "Mã gồm 3–32 chữ không dấu, số, gạch dưới hoặc gạch ngang.")]
    public string Code { get; set; } = "";
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [StringLength(400)] public string? Description { get; set; }
    [Required, RegularExpression("FixedAmount|Percentage")] public string DiscountType { get; set; } = "FixedAmount";
    [Range(typeof(decimal), "0.01", "1000000000")] public decimal DiscountValue { get; set; }
    [Range(typeof(decimal), "1", "1000000000")] public decimal? MaxDiscount { get; set; }
    [Range(typeof(decimal), "0", "1000000000000")] public decimal MinOrderAmount { get; set; }
    [Required] public DateOnly? StartDate { get; set; } = VoucherDefinition.Today;
    [Required] public DateOnly? ExpiryDate { get; set; } = VoucherDefinition.Today.AddDays(30);
    [Range(1, 1000000)] public int UsageLimit { get; set; } = 100;
    [Range(1, 1000000)] public int PerAccountLimit { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool Public { get; set; }
    [StringLength(1200)] public string? Benefits { get; set; }
    public List<string> ProductIds { get; set; } = [];
}

public sealed class VoucherDefinition : VoucherInput
{
    public int UsedCount { get; set; }
    public int Remaining => Math.Max(0, UsageLimit - UsedCount);
    public static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
    public bool Available => Enabled && StartDate <= Today && ExpiryDate >= Today && Remaining > 0;
    public string StatusLabel => !Enabled ? "Đã tắt" : StartDate > Today ? "Chưa bắt đầu" : ExpiryDate < Today ? "Đã kết thúc" : Remaining == 0 ? "Hết lượt" : "Đang áp dụng";
    public string DiscountLabel => DiscountType == "Percentage" ? $"Giảm {DiscountValue:0.##}%" : $"Giảm {DiscountValue.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))} ₫";
    public IEnumerable<string> BenefitLines => (Benefits ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public bool AppliesTo(string productId) => ProductIds.Count == 0 || ProductIds.Contains(productId, StringComparer.OrdinalIgnoreCase);
}

public sealed class VoucherFormViewModel
{
    public VoucherInput Input { get; set; } = new();
    public List<DienThoaiModel> Products { get; set; } = [];
    public bool Editing { get; set; }
}

public sealed record CheckoutQuote(decimal Subtotal, decimal DiscountAmount, decimal Total, string VoucherCode, string VoucherTitle, List<BuyerOrderItem> Lines);
public sealed class CartQuoteInput
{
    [StringLength(32)] public string? VoucherCode { get; set; }
    public List<CustomerOrderRequestItem> Items { get; set; } = [];
}
