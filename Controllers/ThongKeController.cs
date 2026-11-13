using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;
using System.Globalization;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ThongKeController(DienThoaiXmlService phones, HoaDonXmlService invoices, CustomerOrderService orders, SupportChatService chat, AccountXmlService accounts) : Controller
{
    public IActionResult Index(int days = 30, string? from = null, string? to = null)
    {
        var custom = days == 0 || !string.IsNullOrWhiteSpace(from) || !string.IsNullOrWhiteSpace(to);
        if (days is not (7 or 30 or 90)) days = 30;
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var start = today.AddDays(1 - days);
        var end = today;
        if (custom)
        {
            if (DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var requestedStart)
                && DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var requestedEnd)
                && requestedStart <= requestedEnd && requestedEnd <= today && requestedEnd.DayNumber - requestedStart.DayNumber < 366
                && requestedStart.DayNumber >= requestedEnd.DayNumber - requestedStart.DayNumber + 1)
            {
                start = requestedStart; end = requestedEnd; days = end.DayNumber - start.DayNumber + 1;
            }
            else
            {
                custom = false;
                ModelState.AddModelError("", "Chọn đủ ngày bắt đầu/kết thúc, không sau hôm nay, tối đa 366 ngày và đủ khoảng thời gian cho kỳ so sánh. Đang hiển thị kỳ mặc định.");
            }
        }
        var previousStart = start.AddDays(-days);
        var previousEnd = start.AddDays(-1);
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
        bool InPeriod(DateOnly day) => day >= start && day <= end;
        var periodInvoices = allInvoices.Where(x => InPeriod(DateOnly.FromDateTime(x.NgayLap))).ToList();
        var delivered = allOrders.Where(x => x.Status == "Delivered").Select(x => (Order: x, Day: DeliveredOn(x))).ToList();
        var periodDelivered = delivered.Where(x => x.Day.HasValue && InPeriod(x.Day.Value)).Select(x => x.Order).ToList();
        var previousInvoices = allInvoices.Where(x => DateOnly.FromDateTime(x.NgayLap) >= previousStart && DateOnly.FromDateTime(x.NgayLap) <= previousEnd).ToList();
        var previousDelivered = delivered.Where(x => x.Day >= previousStart && x.Day <= previousEnd).Select(x => x.Order).ToList();
        var periodOrders = allOrders.Where(x => InPeriod(DateOnly.FromDateTime(x.CreatedAt.ToOffset(TimeSpan.FromHours(7)).DateTime))).ToList();
        var productMap = products.ToDictionary(x => x.MaSP, StringComparer.OrdinalIgnoreCase);
        var sales = periodInvoices.SelectMany(x => x.ChiTiet.Select(i => new DashboardProduct(i.MaSP, i.TenSP, i.SoLuong, i.ThanhTien)))
            .Concat(periodDelivered.SelectMany(x => x.Items.Select(i => new DashboardProduct(i.ProductId, i.Name, i.Quantity, i.Total)))).ToList();
        var brands = products.Select(p => (Code: p.MaHang, Name: p.TenHang))
            .Concat(sales.Where(s => !productMap.ContainsKey(s.Id)).Select(_ => (Code: "OTHER", Name: "Không phân loại")))
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
        var model = new ThongKeViewModel
        {
            SoNgay = days,
            TuChon = custom,
            TuNgay = start,
            DenNgay = end,
            TuNgayKyTruoc = previousStart,
            DenNgayKyTruoc = previousEnd,
            KyTruoc = new DashboardPeriod(previousInvoices.Sum(Revenue) + previousDelivered.Sum(x => x.Total), previousInvoices.Count + previousDelivered.Count,
                previousInvoices.Sum(x => x.ChiTiet.Sum(i => i.SoLuong)) + previousDelivered.Sum(x => x.Items.Sum(i => i.Quantity))),
            NgayGiaoDichGanNhat = allInvoices.Select(x => (DateOnly?)DateOnly.FromDateTime(x.NgayLap)).Concat(delivered.Select(x => x.Day)).Where(x => x <= today).DefaultIfEmpty().Max(),
            TrangThaiDon = new[] { "Pending", "Confirmed", "Shipping", "Delivered", "Cancelled" }.Concat(periodOrders.Select(x => x.Status)).Distinct()
                .Select(status => new DashboardOrderStatus(status, BuyerOrder.StatusName(status), periodOrders.Count(x => x.Status == status), periodOrders.Where(x => x.Status == status).Sum(x => x.Total))).ToList(),
            TongSoSanPham = products.Count,
            TongSoHoaDon = periodInvoices.Count,
            SoDonDaGiao = periodDelivered.Count,
            SoMayDaBan = sales.Sum(x => x.Quantity),
            SoHoaDonLechTong = periodInvoices.Concat(previousInvoices).Count(x => x.TongTien != Revenue(x)),
            SoDonThieuNgayGiao = delivered.Count(x => !x.Day.HasValue),
            TongTonKho = products.Sum(x => x.SoLuongTon),
            GiaTriTonKho = products.Sum(x => x.SoLuongTon * x.GiaBan),
            SoKhachHang = customerCount,
            SoSanPhamHetHang = products.Count(x => x.SoLuongTon == 0),
            SoSanPhamSapHetHang = products.Count(x => x.SoLuongTon is > 0 and < 5),
            SanPhamSapHetHang = products.Where(x => x.SoLuongTon < 5).OrderBy(x => x.SoLuongTon).ThenBy(x => x.TenSP).Take(8).ToList(),
            SoDonTrongKy = periodOrders.Count,
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
                var previousDate = previousStart.AddDays(offset);
                var previousPos = previousInvoices.Where(x => DateOnly.FromDateTime(x.NgayLap) == previousDate).ToList();
                var previousOnline = previousDelivered.Where(x => DeliveredOn(x) == previousDate).ToList();
                return new DashboardDay(date.ToString("dd/MM/yyyy"), pos.Sum(Revenue), online.Sum(x => x.Total),
                    pos.Count + online.Count, pos.Sum(x => x.ChiTiet.Sum(i => i.SoLuong)) + online.Sum(x => x.Items.Sum(i => i.Quantity)),
                    previousDate.ToString("dd/MM/yyyy"), previousPos.Sum(Revenue) + previousOnline.Sum(x => x.Total), previousPos.Count + previousOnline.Count,
                    previousPos.Sum(x => x.ChiTiet.Sum(i => i.SoLuong)) + previousOnline.Sum(x => x.Items.Sum(i => i.Quantity)));
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
                    TongTienBan = sold.Sum(x => x.Amount),
                    DoanhThuPos = periodInvoices.SelectMany(x => x.ChiTiet).Where(x => string.Equals(productMap.GetValueOrDefault(x.MaSP)?.MaHang ?? "OTHER", brand.Code, StringComparison.OrdinalIgnoreCase)).Sum(x => x.ThanhTien),
                    DoanhThuOnline = periodDelivered.SelectMany(x => x.Items).Where(x => string.Equals(productMap.GetValueOrDefault(x.ProductId)?.MaHang ?? "OTHER", brand.Code, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Total)
                };
            }).OrderByDescending(x => x.TongTienBan).ThenBy(x => x.TenHang).ToList()
        };
        model.TongDoanhThu = model.DoanhThuPos + model.DoanhThuOnline;
        return View(model);
    }
}
