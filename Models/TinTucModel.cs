namespace QuanLyBanDienThoai.Models
{
    public class TinTucModel
    {
        public string MaTT { get; set; } = string.Empty;
        public string TieuDe { get; set; } = string.Empty;
        public string TomTat { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty;
        public DateTime NgayDang { get; set; } = DateTime.Now;
        public string DanhMuc { get; set; } = "Tin công nghệ";
        public string TacGia { get; set; } = "Ban biên tập";
        public string LienKetGoc { get; set; } = string.Empty;
        public int LuotXem { get; set; } = 0;
        public bool NoiBat { get; set; } = false;
    }
}
