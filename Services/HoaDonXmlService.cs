using System.Xml.Linq;
using System.Xml.XPath;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services
{
    public class HoaDonXmlService
    {
        private readonly string _xmlPath;
        private readonly string _xsdPath;
        private readonly DienThoaiXmlService _dienThoaiService;
        private readonly XmlValidationService _validationService;

        public HoaDonXmlService(IWebHostEnvironment env, DienThoaiXmlService dienThoaiService, XmlValidationService validationService)
        {
            _xmlPath = Path.Combine(env.ContentRootPath, "Data", "HoaDon.xml");
            _xsdPath = Path.Combine(env.ContentRootPath, "Data", "HoaDon.xsd");
            _dienThoaiService = dienThoaiService;
            _validationService = validationService;
        }

        public string GetXmlPath() => _xmlPath;
        public string GetXsdPath() => _xsdPath;

        public List<HoaDonModel> GetAll()
        {
            if (!File.Exists(_xmlPath)) return new List<HoaDonModel>();

            XDocument doc = XDocument.Load(_xmlPath);
            return doc.Root?.Elements("HoaDon").Select(MapFromXElement).OrderByDescending(x => x.NgayLap).ToList() ?? new List<HoaDonModel>();
        }

        public HoaDonModel? GetById(string maHd)
        {
            if (!File.Exists(_xmlPath)) return null;

            XDocument doc = XDocument.Load(_xmlPath);
            XElement? elem = doc.XPathSelectElement($"//HoaDon[MaHD='{maHd}']");
            return elem != null ? MapFromXElement(elem) : null;
        }

        public (bool Success, string Message, string MaHD, ValidationResultModel? Validation) CreateInvoice(CreateHoaDonInputModel input)
        {
            if (!File.Exists(_xmlPath)) return (false, "Tệp XML hóa đơn không tồn tại.", "", null);
            if (input.Items == null || input.Items.Count == 0) return (false, "Vui lòng chọn ít nhất 1 sản phẩm!", "", null);

            XDocument doc = XDocument.Load(_xmlPath);

            // Tự động sinh mã HD00x
            int maxId = 0;
            foreach (var el in doc.Root?.Elements("HoaDon") ?? Enumerable.Empty<XElement>())
            {
                string code = el.Element("MaHD")?.Value ?? "";
                if (code.StartsWith("HD") && int.TryParse(code.Substring(2), out int idVal))
                {
                    if (idVal > maxId) maxId = idVal;
                }
            }
            string newMaHD = $"HD{(maxId + 1):D3}";

            var hoaDonModel = new HoaDonModel
            {
                MaHD = newMaHD,
                NgayLap = DateTime.Now,
                KhachHang = new KhachHangModel
                {
                    TenKH = input.TenKH,
                    SoDienThoai = input.SoDienThoai,
                    DiaChi = input.DiaChi
                },
                NhanVien = string.IsNullOrWhiteSpace(input.NhanVien) ? "Nhân viên Bán hàng" : input.NhanVien,
                GhiChu = input.GhiChu,
                ChiTiet = new List<ChiTietHoaDonModel>()
            };

            decimal tongTien = 0;

            // Xử lý từng sản phẩm trong hóa đơn
            foreach (var item in input.Items)
            {
                var dt = _dienThoaiService.GetById(item.MaSP);
                if (dt == null) return (false, $"Không tìm thấy sản phẩm '{item.MaSP}'.", "", null);

                if (dt.SoLuongTon < item.SoLuong)
                {
                    return (false, $"Sản phẩm '{dt.TenSP}' chỉ còn tồn {dt.SoLuongTon} cái (Yêu cầu: {item.SoLuong}).", "", null);
                }

                var ct = new ChiTietHoaDonModel
                {
                    MaSP = dt.MaSP,
                    TenSP = dt.TenSP,
                    SoLuong = item.SoLuong,
                    DonGia = dt.GiaBan
                };
                hoaDonModel.ChiTiet.Add(ct);
                tongTien += ct.ThanhTien;
            }

            hoaDonModel.TongTien = tongTien;

            XElement newElement = MapToXElement(hoaDonModel);
            doc.Root?.Add(newElement);

            // Validate XSD với HoaDon.xsd
            string tempXml = doc.ToString();
            var valResult = _validationService.ValidateXmlContent(tempXml, _xsdPath);
            if (!valResult.IsValid)
            {
                return (false, "Lỗi lập hóa đơn do vi phạm XSD Schema!", "", valResult);
            }

            // Lưu XML Hóa đơn
            doc.Save(_xmlPath);

            // Cập nhật giảm số lượng tồn kho điện thoại
            foreach (var ct in hoaDonModel.ChiTiet)
            {
                _dienThoaiService.UpdateStock(ct.MaSP, ct.SoLuong);
            }

            return (true, $"Lập hóa đơn {newMaHD} thành công!", newMaHD, valResult);
        }

        private HoaDonModel MapFromXElement(XElement el)
        {
            var kh = el.Element("KhachHang");
            var details = el.Element("ChiTietHoaDon")?.Elements("ChiTiet").Select(c => new ChiTietHoaDonModel
            {
                MaSP = c.Element("MaSP")?.Value ?? "",
                TenSP = c.Element("TenSP")?.Value ?? "",
                SoLuong = int.TryParse(c.Element("SoLuong")?.Value, out int sl) ? sl : 0,
                DonGia = decimal.TryParse(c.Element("DonGia")?.Value, out decimal dg) ? dg : 0
            }).ToList() ?? new List<ChiTietHoaDonModel>();

            return new HoaDonModel
            {
                MaHD = el.Element("MaHD")?.Value ?? "",
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
                        new XElement("ThanhTien", c.ThanhTien)
                    ))
                ),
                new XElement("TongTien", m.TongTien),
                new XElement("GhiChu", m.GhiChu ?? "")
            );
        }
    }
}
