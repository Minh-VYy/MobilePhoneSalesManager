namespace QuanLyBanDienThoai.Models
{
    public class DienThoaiModel
    {
        public string MaSP { get; set; } = string.Empty;
        public string TenSP { get; set; } = string.Empty;
        public string MaHang { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }
        public ThongSoKyThuatModel ThongSo { get; set; } = new ThongSoKyThuatModel();
        public string HinhAnh { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
    }

    public class ThongSoKyThuatModel
    {
        public string ManHinh { get; set; } = string.Empty;
        public string RAM { get; set; } = string.Empty;
        public string BoNhoTrong { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string Chip { get; set; } = string.Empty;
    }

    public class SearchFilterViewModel
    {
        public string Keyword { get; set; } = string.Empty;
        public string MaHang { get; set; } = string.Empty;
        public decimal? MinGia { get; set; }
        public decimal? MaxGia { get; set; }
        public List<DienThoaiModel> Items { get; set; } = new List<DienThoaiModel>();
    }
}
