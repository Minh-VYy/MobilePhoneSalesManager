namespace QuanLyBanDienThoai.Models
{
    public class HoaDonModel
    {
        public string MaHD { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; } = DateTime.Now;
        public KhachHangModel KhachHang { get; set; } = new KhachHangModel();
        public string NhanVien { get; set; } = string.Empty;
        public List<ChiTietHoaDonModel> ChiTiet { get; set; } = new List<ChiTietHoaDonModel>();
        public decimal TongTien { get; set; }
        public string GhiChu { get; set; } = string.Empty;
    }

    public class KhachHangModel
    {
        public string TenKH { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;
    }

    public class ChiTietHoaDonModel
    {
        public string MaSP { get; set; } = string.Empty;
        public string TenSP { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }

    public class CreateHoaDonItemInput
    {
        public string MaSP { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }

    public class CreateHoaDonInputModel
    {
        public string TenKH { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;
        public string NhanVien { get; set; } = "Nhân viên Cửa hàng";
        public string GhiChu { get; set; } = string.Empty;
        public List<CreateHoaDonItemInput> Items { get; set; } = new List<CreateHoaDonItemInput>();
    }
}
