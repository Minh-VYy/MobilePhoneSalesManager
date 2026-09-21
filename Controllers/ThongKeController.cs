using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly DienThoaiXmlService _dienThoaiService;
        private readonly HoaDonXmlService _hoaDonService;

        public ThongKeController(DienThoaiXmlService dienThoaiService, HoaDonXmlService hoaDonService)
        {
            _dienThoaiService = dienThoaiService;
            _hoaDonService = hoaDonService;
        }

        public IActionResult Index()
        {
            var phones = _dienThoaiService.GetAll();
            var invoices = _hoaDonService.GetAll();

            int tongSp = phones.Count;
            int tongHd = invoices.Count;
            decimal tongDoanhThu = invoices.Sum(x => x.TongTien);

            // Sản phẩm sắp hết hàng (SoLuongTon < 5)
            var sapHetHang = phones.Where(x => x.SoLuongTon < 5).ToList();

            // Thống kê doanh thu & bán theo hãng
            var hangStats = new List<ThongKeTheoHangModel>();
            var hangGroup = phones.GroupBy(x => x.MaHang);

            foreach (var group in hangGroup)
            {
                string maHang = group.Key;
                string tenHang = group.First().TenHang;
                int tongTon = group.Sum(x => x.SoLuongTon);

                // Tính tổng số lượng đã bán của hãng qua tất cả hóa đơn
                int daBan = 0;
                decimal doanhThuHang = 0;

                foreach (var inv in invoices)
                {
                    foreach (var item in inv.ChiTiet)
                    {
                        var p = phones.FirstOrDefault(x => x.MaSP == item.MaSP);
                        if (p != null && p.MaHang == maHang)
                        {
                            daBan += item.SoLuong;
                            doanhThuHang += item.ThanhTien;
                        }
                    }
                }

                hangStats.Add(new ThongKeTheoHangModel
                {
                    MaHang = maHang,
                    TenHang = tenHang,
                    SoLuongTon = tongTon,
                    SoLuongDaBan = daBan,
                    TongTienBan = doanhThuHang
                });
            }

            var model = new ThongKeViewModel
            {
                TongSoSanPham = tongSp,
                TongSoHoaDon = tongHd,
                TongDoanhThu = tongDoanhThu,
                SoSanPhamSapHetHang = sapHetHang.Count,
                SanPhamSapHetHang = sapHetHang,
                DoanhThuTheoHang = hangStats
            };

            return View(model);
        }
    }
}
