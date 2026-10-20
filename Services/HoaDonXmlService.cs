using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services
{
    public class HoaDonXmlService
    {
        private readonly string _xmlPath;
        private readonly string _xsdPath;
        private readonly PrivateXmlStore _store;
        private readonly XmlValidationService _validationService;

        public HoaDonXmlService(IWebHostEnvironment env, PrivateXmlStore store, XmlValidationService validationService)
        {
            _xmlPath = Path.Combine(env.ContentRootPath, "Data", "HoaDon.xml");
            _xsdPath = Path.Combine(env.ContentRootPath, "Data", "HoaDon.xsd");
            _store = store;
            _validationService = validationService;
        }

        public string GetXmlPath() => _xmlPath;
        public string GetXsdPath() => _xsdPath;

        public List<HoaDonModel> GetAll()
        {
            return _store.Read("HoaDon.xml", "DanhSachHoaDon", root => root.Elements("HoaDon").Select(MapFromXElement).OrderByDescending(x => x.NgayLap).ToList());
        }

        public HoaDonModel? GetById(string maHd)
        {
            return _store.Read("HoaDon.xml", "DanhSachHoaDon", root => root.Elements("HoaDon").Where(x => (string?)x.Element("MaHD") == maHd).Select(MapFromXElement).FirstOrDefault());
        }

        public (bool Success, string Message, string MaHD, ValidationResultModel? Validation) CreateInvoice(CreateHoaDonInputModel input)
        {
            ValidationResultModel? result = null;
            try
            {
                if (!Guid.TryParse(input.CheckoutToken, out _)) throw new InvalidOperationException("Mã phiên lập hóa đơn không hợp lệ. Hãy mở lại màn hình POS.");
                if (!input.PaymentConfirmed || input.PaymentMethod is not ("Cash" or "BankTransfer")) throw new InvalidOperationException("Xác nhận thanh toán và chọn tiền mặt hoặc chuyển khoản trước khi xuất hóa đơn.");
                if (input.Items == null || input.Items.Count is < 1 or > 100 || input.Items.Any(x => x.SoLuong is < 1 or > 999))
                    throw new InvalidOperationException("Chọn từ 1 đến 100 phiên bản, số lượng mỗi phiên bản từ 1 đến 999.");
                return _store.UpdatePointOfSale<(bool, string, string, ValidationResultModel?)>((invoices, products, units) =>
                {
                    var existing = invoices.Elements("HoaDon").FirstOrDefault(x => (string?)x.Element("CheckoutToken") == input.CheckoutToken);
                    if (existing != null) return (true, "Hóa đơn này đã được lưu trước đó.", (string)existing.Element("MaHD")!, null);
                    var nextId = invoices.Elements("HoaDon").Select(x => int.TryParse(((string?)x.Element("MaHD") ?? "").Replace("HD", ""), out var id) ? id : 0).DefaultIfEmpty(0).Max() + 1;
                    var invoice = new HoaDonModel
                    {
                        MaHD = $"HD{nextId:D3}",
                        CheckoutToken = input.CheckoutToken,
                        PaymentMethod = input.PaymentMethod,
                        NgayLap = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime,
                        KhachHang = new KhachHangModel { TenKH = input.TenKH.Trim(), SoDienThoai = input.SoDienThoai.Trim(), DiaChi = input.DiaChi.Trim() },
                        NhanVien = input.NhanVien,
                        GhiChu = input.GhiChu
                    };
                    var lines = input.Items.GroupBy(x => x.MaSP, StringComparer.OrdinalIgnoreCase)
                        .Select(group => new { Id = group.Key, Quantity = group.Sum(x => x.SoLuong), Codes = group.SelectMany(x => x.UnitCodes ?? []).Select(StockUnitService.Normalize).ToList() }).ToList();
                    var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var line in lines)
                    {
                        var phone = products.Elements("DienThoai").FirstOrDefault(x => string.Equals((string?)x.Element("MaSP"), line.Id, StringComparison.OrdinalIgnoreCase));
                        if (phone == null) throw new InvalidOperationException($"Sản phẩm {line.Id} không còn tồn tại.");
                        if (line.Quantity > 999 || (int?)phone.Element("SoLuongTon") < line.Quantity)
                            throw new InvalidOperationException($"Sản phẩm {(string?)phone.Element("TenSP")} không đủ tồn kho cho số lượng đã chọn.");
                        if (line.Codes.Count != line.Quantity) throw new InvalidOperationException($"Cần quét đủ {line.Quantity} mã IMEI/serial cho {(string?)phone.Element("TenSP")} trước khi xuất hóa đơn.");
                        var canonicalCodes = new List<string>();
                        foreach (var code in line.Codes)
                        {
                            var unit = StockUnitService.Find(units, code) ?? throw new InvalidOperationException($"Mã máy {code} chưa được đăng ký trong kho.");
                            var canonical = (string)unit.Attribute("code")!;
                            if ((string?)unit.Attribute("productId") != (string?)phone.Element("MaSP")) throw new InvalidOperationException($"Mã máy {code} không thuộc phiên bản đang chọn.");
                            if ((string?)unit.Attribute("status") != "Available") throw new InvalidOperationException($"Máy {code} đã bán hoặc đang được giữ cho một đơn trực tuyến. Không thể bán lại.");
                            if (!usedCodes.Add(canonical)) throw new InvalidOperationException($"Máy {canonical} được quét trùng trong hóa đơn.");
                            canonicalCodes.Add(canonical);
                        }
                        invoice.ChiTiet.Add(new ChiTietHoaDonModel
                        {
                            MaSP = (string)phone.Element("MaSP")!,
                            TenSP = (string)phone.Element("TenSP")!,
                            SoLuong = line.Quantity,
                            UnitCodes = canonicalCodes,
                            DonGia = (decimal)phone.Element("GiaBan")!
                        });
                    }
                    invoice.TongTien = invoice.ChiTiet.Sum(x => x.ThanhTien);
                    var element = MapToXElement(invoice);
                    var proposed = new XElement(invoices); proposed.Add(element);
                    result = _validationService.ValidateXmlContent(proposed.ToString(), _xsdPath);
                    if (!result.IsValid) throw new InvalidOperationException("Thông tin hóa đơn chưa đáp ứng ràng buộc XSD. Hãy kiểm tra lại các trường bên dưới.");
                    invoices.Add(element);
                    foreach (var line in invoice.ChiTiet)
                    {
                        var phone = products.Elements("DienThoai").First(x => (string?)x.Element("MaSP") == line.MaSP);
                        phone.SetElementValue("SoLuongTon", (int)phone.Element("SoLuongTon")! - line.SoLuong);
                        foreach (var code in line.UnitCodes) StockUnitService.MarkSold(StockUnitService.Find(units, code)!, invoice.MaHD, "POS", line.DonGia, invoice.NhanVien);
                    }
                    return (true, $"Đã lưu hóa đơn {invoice.MaHD} và cập nhật tồn kho.", invoice.MaHD, result);
                });
            }
            catch (InvalidOperationException error) { return (false, error.Message, "", result); }
        }

        private HoaDonModel MapFromXElement(XElement el)
        {
            var kh = el.Element("KhachHang");
            var details = el.Element("ChiTietHoaDon")?.Elements("ChiTiet").Select(c => new ChiTietHoaDonModel
            {
                MaSP = c.Element("MaSP")?.Value ?? "",
                TenSP = c.Element("TenSP")?.Value ?? "",
                SoLuong = int.TryParse(c.Element("SoLuong")?.Value, out int sl) ? sl : 0,
                UnitCodes = c.Element("UnitCodes")?.Elements("Code").Select(x => x.Value).ToList() ?? [],
                DonGia = decimal.TryParse(c.Element("DonGia")?.Value, out decimal dg) ? dg : 0
            }).ToList() ?? new List<ChiTietHoaDonModel>();

            return new HoaDonModel
            {
                MaHD = el.Element("MaHD")?.Value ?? "",
                CheckoutToken = el.Element("CheckoutToken")?.Value ?? "",
                PaymentMethod = el.Element("PaymentMethod")?.Value ?? "Unknown",
                NgayLap = DateTime.TryParse(el.Element("NgayLap")?.Value, out DateTime d) ? d : DateTime.Now,
                KhachHang = new KhachHangModel
                {
                    TenKH = kh?.Element("TenKH")?.Value ?? "",
                    SoDienThoai = kh?.Element("SoDienThoai")?.Value ?? "",
                    DiaChi = kh?.Element("DiaChi")?.Value ?? ""
                },
                NhanVien = el.Element("NhanVien")?.Value ?? "",
                ChiTiet = details,
                TongTien = decimal.TryParse(el.Element("TongTien")?.Value, out decimal tt) ? tt : 0,
                GhiChu = el.Element("GhiChu")?.Value ?? ""
            };
        }

        private XElement MapToXElement(HoaDonModel m)
        {
            return new XElement("HoaDon",
                new XElement("MaHD", m.MaHD),
                new XElement("NgayLap", m.NgayLap.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("KhachHang",
                    new XElement("TenKH", m.KhachHang.TenKH),
                    new XElement("SoDienThoai", m.KhachHang.SoDienThoai),
                    new XElement("DiaChi", m.KhachHang.DiaChi)
                ),
                new XElement("NhanVien", m.NhanVien),
                new XElement("ChiTietHoaDon",
                    m.ChiTiet.Select(c => new XElement("ChiTiet",
                        new XElement("MaSP", c.MaSP),
                        new XElement("TenSP", c.TenSP),
                        new XElement("SoLuong", c.SoLuong),
                        new XElement("DonGia", c.DonGia),
                        new XElement("ThanhTien", c.ThanhTien),
                        new XElement("UnitCodes", c.UnitCodes.Select(code => new XElement("Code", code)))
                    ))
                ),
                new XElement("TongTien", m.TongTien),
                new XElement("GhiChu", m.GhiChu ?? ""),
                new XElement("CheckoutToken", m.CheckoutToken),
                new XElement("PaymentMethod", m.PaymentMethod)
            );
        }
    }
}
