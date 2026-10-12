namespace QuanLyBanDienThoai.Models;

public class DienThoaiChiTietViewModel
{
    public DienThoaiModel SanPham { get; set; } = new();
    public List<DienThoaiModel> SanPhamLienQuan { get; set; } = new();
    public string DanhMuc { get; set; } = "ALL";
    public PhoneDetailMetadata Metadata { get; set; } = new();
    public List<PhoneDetailMetadata> BienThe { get; set; } = new();
    public List<PhoneVariantOptionViewModel> VariantOptions { get; set; } = new();
    public List<ProductReviewModel> DanhGia { get; set; } = new();
    public double DiemDanhGia => DanhGia.Count == 0 ? 0 : Math.Round(DanhGia.Average(x => x.Rating), 1);
}

public class PhoneVariantOptionViewModel
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ModelSeries { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Stock { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ColorCode { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public string ConditionGrade { get; set; } = string.Empty;
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionValue { get; set; } = string.Empty;
    public int WarrantyMonths { get; set; }
    public string WarrantyText { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public string StorageLabel { get; set; } = string.Empty;
    public string Screen { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public string Chipset { get; set; } = string.Empty;
    public string Camera { get; set; } = string.Empty;
    public string Battery { get; set; } = string.Empty;
    public string ChargingTechnology { get; set; } = string.Empty;
    public string Ports { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string DetailsUrl { get; set; } = string.Empty;
    public string SmsUrl { get; set; } = string.Empty;
}
