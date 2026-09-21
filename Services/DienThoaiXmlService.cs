using System.Xml.Linq;
using System.Xml.XPath;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services
{
    public class DienThoaiXmlService
    {
        private readonly string _xmlPath;
        private readonly string _xsdPath;
        private readonly XmlValidationService _validationService;

        public DienThoaiXmlService(IWebHostEnvironment env, XmlValidationService validationService)
        {
            _xmlPath = Path.Combine(env.ContentRootPath, "Data", "DienThoai.xml");
            _xsdPath = Path.Combine(env.ContentRootPath, "Data", "DienThoai.xsd");
            _validationService = validationService;
        }

        public string GetXmlPath() => _xmlPath;
        public string GetXsdPath() => _xsdPath;

        public List<DienThoaiModel> GetAll()
        {
            if (!File.Exists(_xmlPath)) return new List<DienThoaiModel>();

            XDocument doc = XDocument.Load(_xmlPath);
            return doc.Root?.Elements("DienThoai").Select(MapFromXElement).ToList() ?? new List<DienThoaiModel>();
        }

        public DienThoaiModel? GetById(string maSp)
        {
            if (!File.Exists(_xmlPath)) return null;

            XDocument doc = XDocument.Load(_xmlPath);
            XElement? elem = doc.XPathSelectElement($"//DienThoai[MaSP='{maSp}']");
            return elem != null ? MapFromXElement(elem) : null;
        }

        public List<DienThoaiModel> SearchAndFilter(string keyword, string maHang, decimal? minGia, decimal? maxGia)
        {
            if (!File.Exists(_xmlPath)) return new List<DienThoaiModel>();

            XDocument doc = XDocument.Load(_xmlPath);
            
            // Xây dựng câu truy vấn XPath động
            List<string> conditions = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(maHang) && maHang != "ALL")
            {
                conditions.Add($"MaHang='{maHang.ToUpper()}'");
            }

            if (minGia.HasValue)
            {
                conditions.Add($"GiaBan >= {minGia.Value}");
            }

            if (maxGia.HasValue)
            {
                conditions.Add($"GiaBan <= {maxGia.Value}");
            }

            string xpathQuery = "//DienThoai";
            if (conditions.Count > 0)
            {
                xpathQuery += "[" + string.Join(" and ", conditions) + "]";
            }

            var elements = doc.XPathSelectElements(xpathQuery).Select(MapFromXElement);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                elements = elements.Where(x => 
                    x.TenSP.ToLower().Contains(kw) || 
                    x.MaSP.ToLower().Contains(kw) || 
                    x.TenHang.ToLower().Contains(kw) ||
                    x.MoTa.ToLower().Contains(kw));
            }

            return elements.ToList();
        }

        public (bool Success, string Message, ValidationResultModel? Validation) Add(DienThoaiModel model)
        {
            if (!File.Exists(_xmlPath)) return (false, "Không tìm thấy tệp XML dữ liệu.", null);

            XDocument doc = XDocument.Load(_xmlPath);

            // Tự động sinh mã SP00x nếu chưa có
            if (string.IsNullOrWhiteSpace(model.MaSP))
            {
                int maxId = 0;
                foreach (var el in doc.Root?.Elements("DienThoai") ?? Enumerable.Empty<XElement>())
                {
                    string code = el.Element("MaSP")?.Value ?? "";
                    if (code.StartsWith("SP") && int.TryParse(code.Substring(2), out int idVal))
                    {
                        if (idVal > maxId) maxId = idVal;
                    }
                }
                model.MaSP = $"SP{(maxId + 1):D3}";
            }

            // Kiểm tra trùng mã
            if (doc.XPathSelectElement($"//DienThoai[MaSP='{model.MaSP}']") != null)
            {
                return (false, $"Mã sản phẩm '{model.MaSP}' đã tồn tại.", null);
            }

            XElement newElement = MapToXElement(model);
            doc.Root?.Add(newElement);

            // Validate với XSD trước khi lưu tệp chính thức
            string tempXml = doc.ToString();
            var valResult = _validationService.ValidateXmlContent(tempXml, _xsdPath);
            if (!valResult.IsValid)
            {
                return (false, "Dữ liệu điện thoại không đáp ứng ràng buộc của XSD Schema!", valResult);
            }

            doc.Save(_xmlPath);
            return (true, "Thêm sản phẩm điện thoại mới thành công!", valResult);
        }

        public (bool Success, string Message, ValidationResultModel? Validation) Update(DienThoaiModel model)
        {
            if (!File.Exists(_xmlPath)) return (false, "Không tìm thấy tệp XML dữ liệu.", null);

            XDocument doc = XDocument.Load(_xmlPath);
            XElement? elem = doc.XPathSelectElement($"//DienThoai[MaSP='{model.MaSP}']");

            if (elem == null)
            {
                return (false, $"Không tìm thấy sản phẩm có mã '{model.MaSP}'.", null);
            }

            XElement updatedElement = MapToXElement(model);
            elem.ReplaceWith(updatedElement);

            // Validate XSD
            string tempXml = doc.ToString();
            var valResult = _validationService.ValidateXmlContent(tempXml, _xsdPath);
            if (!valResult.IsValid)
            {
                return (false, "Cập nhật thất bại do dữ liệu vi phạm XSD Schema!", valResult);
            }

            doc.Save(_xmlPath);
            return (true, "Cập nhật sản phẩm điện thoại thành công!", valResult);
        }

        public bool Delete(string maSp)
        {
            if (!File.Exists(_xmlPath)) return false;

            XDocument doc = XDocument.Load(_xmlPath);
            XElement? elem = doc.XPathSelectElement($"//DienThoai[MaSP='{maSp}']");

            if (elem == null) return false;

            elem.Remove();
            doc.Save(_xmlPath);
            return true;
        }

        public bool UpdateStock(string maSp, int quantitySold)
        {
            if (!File.Exists(_xmlPath)) return false;

            XDocument doc = XDocument.Load(_xmlPath);
            XElement? elem = doc.XPathSelectElement($"//DienThoai[MaSP='{maSp}']");

            if (elem == null) return false;

            var stockElem = elem.Element("SoLuongTon");
            if (stockElem != null && int.TryParse(stockElem.Value, out int currentStock))
            {
                int newStock = Math.Max(0, currentStock - quantitySold);
                stockElem.Value = newStock.ToString();
                doc.Save(_xmlPath);
                return true;
            }

            return false;
        }

        private DienThoaiModel MapFromXElement(XElement el)
        {
            var thongSo = el.Element("ThongSoKyThuat");
            return new DienThoaiModel
            {
                MaSP = el.Element("MaSP")?.Value ?? "",
                TenSP = el.Element("TenSP")?.Value ?? "",
                MaHang = el.Element("MaHang")?.Value ?? "",
                TenHang = el.Element("TenHang")?.Value ?? "",
                GiaBan = decimal.TryParse(el.Element("GiaBan")?.Value, out decimal g) ? g : 0,
                SoLuongTon = int.TryParse(el.Element("SoLuongTon")?.Value, out int s) ? s : 0,
                ThongSo = new ThongSoKyThuatModel
                {
                    ManHinh = thongSo?.Element("ManHinh")?.Value ?? "",
                    RAM = thongSo?.Element("RAM")?.Value ?? "",
                    BoNhoTrong = thongSo?.Element("BoNhoTrong")?.Value ?? "",
                    Pin = thongSo?.Element("Pin")?.Value ?? "",
                    Chip = thongSo?.Element("Chip")?.Value ?? ""
                },
                HinhAnh = el.Element("HinhAnh")?.Value ?? "",
                MoTa = el.Element("MoTa")?.Value ?? ""
            };
        }

        private XElement MapToXElement(DienThoaiModel m)
        {
            return new XElement("DienThoai",
                new XElement("MaSP", m.MaSP),
                new XElement("TenSP", m.TenSP),
                new XElement("MaHang", m.MaHang),
                new XElement("TenHang", m.TenHang),
                new XElement("GiaBan", m.GiaBan),
                new XElement("SoLuongTon", m.SoLuongTon),
                new XElement("ThongSoKyThuat",
                    new XElement("ManHinh", m.ThongSo.ManHinh),
                    new XElement("RAM", m.ThongSo.RAM),
                    new XElement("BoNhoTrong", m.ThongSo.BoNhoTrong),
                    new XElement("Pin", m.ThongSo.Pin),
                    new XElement("Chip", m.ThongSo.Chip)
                ),
                new XElement("HinhAnh", m.HinhAnh),
                new XElement("MoTa", m.MoTa)
            );
        }
    }
}
