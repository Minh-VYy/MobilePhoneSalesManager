using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ThongKeController(DienThoaiXmlService phones, HoaDonXmlService invoices, CustomerOrderService orders, SupportChatService chat, AccountXmlService accounts) : Controller
{
    public IActionResult Index(int days = 30)
    {
        if (days is not (7 or 30 or 90)) days = 30;
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var start = today.AddDays(1 - days);
        List<DienThoaiModel> products;
        List<HoaDonModel> allInvoices;
        List<BuyerOrder> allOrders;
        List<SupportConversation> messages;
        int customerCount;
        // Read one snapshot, including recovery of interrupted commerce writes.
        lock (PrivateXmlStore.Gate)
        {
            allInvoices = invoices.GetAll();
            products = phones.GetAll();
            allOrders = orders.GetAll();
            messages = chat.Conversations().Where(x => x.Unread > 0).ToList();
            customerCount = accounts.CustomerCount;
        }
        static DateOnly? DeliveredOn(BuyerOrder order) => order.History.LastOrDefault(x => x.Status == "Delivered") is { } entry
            ? DateOnly.FromDateTime(entry.At.ToOffset(TimeSpan.FromHours(7)).DateTime) : null;
        // Use historical line prices for every breakdown, never today's catalogue prices.
        static decimal Revenue(HoaDonModel invoice) => invoice.ChiTiet.Sum(x => x.ThanhTien);
        bool InPeriod(DateOnly day) => day >= start && day <= today;
        var periodInvoices = allInvoices.Where(x => InPeriod(DateOnly.FromDateTime(x.NgayLap))).ToList();
        var delivered = allOrders.Where(x => x.Status == "Delivered").Select(x => (Order: x, Day: DeliveredOn(x))).ToList();
        var periodDelivered = delivered.Where(x => x.Day.HasValue && InPeriod(x.Day.Value)).Select(x => x.Order).ToList();
        var productMap = products.ToDictionary(x => x.MaSP, StringComparer.OrdinalIgnoreCase);
        var sales = periodInvoices.SelectMany(x => x.ChiTiet.Select(i => new DashboardProduct(i.MaSP, i.TenSP, i.SoLuong, i.ThanhTien)))
            .Concat(periodDelivered.SelectMany(x => x.Items.Select(i => new DashboardProduct(i.ProductId, i.Name, i.Quantity, i.UnitPrice * i.Quantity)))).ToList();
        var brands = products.Select(p => (Code: p.MaHang, Name: p.TenHang))
            .Concat(sales.Where(s => !productMap.ContainsKey(s.Id)).Select(_ => (Code: "OTHER", Name: "Không phân loại")))
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
        var model = new ThongKeViewModel
        {
            SoNgay = days,
            TuNgay = start,
            DenNgay = today,
            TongSoSanPham = products.Count,
            TongSoHoaDon = periodInvoices.Count,
            SoDonDaGiao = periodDelivered.Count,
            SoMayDaBan = sales.Sum(x => x.Quantity),
            SoHoaDonLechTong = periodInvoices.Count(x => x.TongTien != Revenue(x)),
            SoDonThieuNgayGiao = delivered.Count(x => !x.Day.HasValue),
            TongTonKho = products.Sum(x => x.SoLuongTon),
            GiaTriTonKho = products.Sum(x => x.SoLuongTon * x.GiaBan),
            SoKhachHang = customerCount,
            SoSanPhamHetHang = products.Count(x => x.SoLuongTon == 0),
            SoSanPhamSapHetHang = products.Count(x => x.SoLuongTon is > 0 and < 5),
            SanPhamSapHetHang = products.Where(x => x.SoLuongTon < 5).OrderBy(x => x.SoLuongTon).ThenBy(x => x.TenSP).Take(8).ToList(),
            SoDonTrongKy = allOrders.Count(x => InPeriod(DateOnly.FromDateTime(x.CreatedAt.ToOffset(TimeSpan.FromHours(7)).DateTime))),
            SoDonChoXacNhan = allOrders.Count(x => x.Status == "Pending"),
            DonChoXacNhan = allOrders.Where(x => x.Status == "Pending").Take(5).ToList(),
            DonGanNhat = allOrders.Take(8).ToList(),
            HoiThoaiChuaDoc = messages.Take(4).ToList(),
            SoTinChuaDoc = messages.Sum(x => x.Unread),
            DoanhThuPos = periodInvoices.Sum(Revenue),
            DoanhThuOnline = periodDelivered.Sum(x => x.Total),
            TheoNgay = Enumerable.Range(0, days).Select(offset =>
            {
                var date = start.AddDays(offset);
                var pos = periodInvoices.Where(x => DateOnly.FromDateTime(x.NgayLap) == date).ToList();
                var online = periodDelivered.Where(x => DeliveredOn(x) == date).ToList();
                return new DashboardDay(date.ToString("dd/MM/yyyy"), pos.Sum(Revenue), online.Sum(x => x.Total),
                    pos.Count + online.Count, pos.Sum(x => x.ChiTiet.Sum(i => i.SoLuong)) + online.Sum(x => x.Items.Sum(i => i.Quantity)));
            }).ToList(),
            SanPhamBanChay = sales.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => new DashboardProduct(group.Key, group.First().Name, group.Sum(x => x.Quantity), group.Sum(x => x.Amount)))
                .OrderByDescending(x => x.Amount).ThenByDescending(x => x.Quantity).ThenBy(x => x.Id).ToList(),
            ThanhToanPos = periodInvoices.GroupBy(x => x.PaymentMethod switch { "Cash" => "Tiền mặt", "BankTransfer" => "Chuyển khoản", _ => "Chưa ghi phương thức" })
                .Select(group => new DashboardPayment(group.Key, group.Count(), group.Sum(Revenue))).OrderByDescending(x => x.Amount).ToList(),
            DoanhThuTheoHang = brands.Select(brand =>
            {
                var sold = sales.Where(s => string.Equals(productMap.GetValueOrDefault(s.Id)?.MaHang ?? "OTHER", brand.Code, StringComparison.OrdinalIgnoreCase)).ToList();
                return new ThongKeTheoHangModel
                {
                    MaHang = brand.Code,
                    TenHang = brand.Name,
                    SoLuongTon = products.Where(p => string.Equals(p.MaHang, brand.Code, StringComparison.OrdinalIgnoreCase)).Sum(p => p.SoLuongTon),
                    SoLuongDaBan = sold.Sum(x => x.Quantity),
                    TongTienBan = sold.Sum(x => x.Amount)
                };
            }).OrderByDescending(x => x.TongTienBan).ThenBy(x => x.TenHang).ToList()
        };
        model.TongDoanhThu = model.DoanhThuPos + model.DoanhThuOnline;
        return View(model);
    }
}
