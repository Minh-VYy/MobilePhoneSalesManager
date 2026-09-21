namespace QuanLyBanDienThoai.Models
{
    public class XmlValidationError
    {
        public int LineNumber { get; set; }
        public int LinePosition { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ValidationResultModel
    {
        public string FileName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<XmlValidationError> Errors { get; set; } = new List<XmlValidationError>();
    }

    public class ThongKeViewModel
    {
        public int TongSoSanPham { get; set; }
        public int TongSoHoaDon { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int SoSanPhamSapHetHang { get; set; }
        public List<DienThoaiModel> SanPhamSapHetHang { get; set; } = new List<DienThoaiModel>();
        public List<ThongKeTheoHangModel> DoanhThuTheoHang { get; set; } = new List<ThongKeTheoHangModel>();
    }

    public class ThongKeTheoHangModel
    {
        public string MaHang { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public int SoLuongTon { get; set; }
        public int SoLuongDaBan { get; set; }
        public decimal TongTienBan { get; set; }
    }
}
