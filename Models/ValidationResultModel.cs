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
        public int SoNgay { get; set; }
        public DateOnly TuNgay { get; set; }
        public DateOnly DenNgay { get; set; }
        public int TongTonKho { get; set; }
        public decimal GiaTriTonKho { get; set; }
        public int SoSanPhamHetHang { get; set; }
        public int SoKhachHang { get; set; }
        public int SoDonTrongKy { get; set; }
        public int SoDonChoXacNhan { get; set; }
        public int SoTinChuaDoc { get; set; }
        public decimal DoanhThuPos { get; set; }
        public decimal DoanhThuOnline { get; set; }
        public int SoDonDaGiao { get; set; }
        public int SoMayDaBan { get; set; }
        public int SoGiaoDich => TongSoHoaDon + SoDonDaGiao;
        public decimal GiaTriTrungBinh => SoGiaoDich == 0 ? 0 : TongDoanhThu / SoGiaoDich;
        public int SoHoaDonLechTong { get; set; }
        public int SoDonThieuNgayGiao { get; set; }
        public List<DashboardProduct> SanPhamBanChay { get; set; } = [];
        public List<DashboardPayment> ThanhToanPos { get; set; } = [];
        public List<DashboardDay> TheoNgay { get; set; } = [];
        public List<BuyerOrder> DonChoXacNhan { get; set; } = [];
        public List<BuyerOrder> DonGanNhat { get; set; } = [];
        public List<SupportConversation> HoiThoaiChuaDoc { get; set; } = [];
    }

    public sealed record DashboardDay(string Label, decimal Pos, decimal Online, int Transactions, int Units);
    public sealed record DashboardProduct(string Id, string Name, int Quantity, decimal Amount);
    public sealed record DashboardPayment(string Label, int Count, decimal Amount);

    public class ThongKeTheoHangModel
    {
        public string MaHang { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public int SoLuongTon { get; set; }
        public int SoLuongDaBan { get; set; }
        public decimal TongTienBan { get; set; }
    }
}
