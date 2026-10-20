using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QuanLyBanDienThoai.Models
{
    public class DienThoaiModel
    {
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [RegularExpression("[A-Z][A-Z0-9-]{2,63}", ErrorMessage = "Mã gồm 3–64 ký tự in hoa, chữ số hoặc dấu gạch ngang; bắt đầu bằng chữ.")]
        public string MaSP { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        public string TenSP { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng chọn hãng sản xuất.")]
        public string MaHang { get; set; } = string.Empty;
        [ValidateNever]
        public string TenHang { get; set; } = string.Empty;
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá bán không được âm.")]
        public decimal GiaBan { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm.")]
        public int SoLuongTon { get; set; }
        public ThongSoKyThuatModel ThongSo { get; set; } = new ThongSoKyThuatModel();
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string HinhAnh { get; set; } = string.Empty;
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string MoTa { get; set; } = string.Empty;
        [ValidateNever]
        public string ModelSeries { get; set; } = string.Empty;
        public int VariantCount { get; set; } = 1;
        [ValidateNever]
        public string VariantSummary { get; set; } = string.Empty;
    }

    public class ThongSoKyThuatModel
    {
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ManHinh { get; set; } = string.Empty;
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string RAM { get; set; } = string.Empty;
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string BoNhoTrong { get; set; } = string.Empty;
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Pin { get; set; } = string.Empty;
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Chip { get; set; } = string.Empty;
    }

    public class SearchFilterViewModel
    {
        public string Keyword { get; set; } = string.Empty;
        public string MaHang { get; set; } = string.Empty;
        public string Category { get; set; } = "ALL";
        public decimal? MinGia { get; set; }
        public decimal? MaxGia { get; set; }
        public List<DienThoaiModel> Items { get; set; } = new List<DienThoaiModel>();
    }
}
