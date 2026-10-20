namespace QuanLyBanDienThoai.Models
{
    public class HomeIndexViewModel
    {
        public List<QuangCaoModel> QuangCao { get; set; } = new();
        public List<DienThoaiModel> FlashSale { get; set; } = new();
        public List<DienThoaiModel> ApplePhones { get; set; } = new();
        public List<DienThoaiModel> AndroidPhones { get; set; } = new();
        public List<DienThoaiModel> Phukien { get; set; } = new();
        public List<TinTucModel> TinTuc { get; set; } = new();
    }
}
