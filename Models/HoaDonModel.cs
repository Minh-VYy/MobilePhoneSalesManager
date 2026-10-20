using System.ComponentModel.DataAnnotations;

namespace QuanLyBanDienThoai.Models
{
    public class HoaDonModel
    {
        public string CheckoutToken { get; set; } = "";
        public string PaymentMethod { get; set; } = "Unknown";
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
        public List<string> UnitCodes { get; set; } = [];
        public string MaSP { get; set; } = string.Empty;
        public string TenSP { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }

    public class CreateHoaDonItemInput
    {
        [MaxLength(999)] public List<string> UnitCodes { get; set; } = [];
        [Required(ErrorMessage = "Thiếu mã sản phẩm.")]
        public string MaSP { get; set; } = string.Empty;
        [Range(1, 999, ErrorMessage = "Số lượng mỗi phiên bản cần từ 1 đến 999.")]
        public int SoLuong { get; set; }
    }

    public class CreateHoaDonInputModel
    {
        [RegularExpression("Cash|BankTransfer"), Required] public string PaymentMethod { get; set; } = "Cash";
        [Range(typeof(bool), "true", "true", ErrorMessage = "Xác nhận đã nhận thanh toán trước khi xuất hóa đơn.")] public bool PaymentConfirmed { get; set; }
        public string CheckoutToken { get; set; } = Guid.NewGuid().ToString("N");
        [Required(ErrorMessage = "Nhập tên khách hàng."), StringLength(100)]
        public string TenKH { get; set; } = string.Empty;
        [Required(ErrorMessage = "Nhập số điện thoại."), RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Số điện thoại Việt Nam gồm 10 số.")]
        public string SoDienThoai { get; set; } = string.Empty;
        [DisplayFormat(ConvertEmptyStringToNull = false), StringLength(250)]
        public string DiaChi { get; set; } = string.Empty;
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string NhanVien { get; set; } = "Nhân viên Cửa hàng";
        [DisplayFormat(ConvertEmptyStringToNull = false), StringLength(1000)]
        public string GhiChu { get; set; } = string.Empty;
        [MinLength(1, ErrorMessage = "Chọn ít nhất một sản phẩm."), MaxLength(100)]
        public List<CreateHoaDonItemInput> Items { get; set; } = new List<CreateHoaDonItemInput>();
    }
}
