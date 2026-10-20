using System.Xml.Linq;
using System.Xml.XPath;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services
{
    public class TinTucXmlService
    {
        private readonly string _xmlPath;
        private readonly string _xsdPath;
        private readonly XmlValidationService _validationService;

        public TinTucXmlService(IWebHostEnvironment env, XmlValidationService validationService)
        {
            _xmlPath = Path.Combine(env.ContentRootPath, "Data", "TinTuc.xml");
            _xsdPath = Path.Combine(env.ContentRootPath, "Data", "TinTuc.xsd");
            _validationService = validationService;
        }

        public string GetXmlPath() => _xmlPath;
        public string GetXsdPath() => _xsdPath;

        public List<TinTucModel> GetAll()
        {
            lock (PrivateXmlStore.Gate)
            {
                if (!File.Exists(_xmlPath)) return new List<TinTucModel>();
                var doc = XDocument.Load(_xmlPath);
                return doc.Root?.Elements("TinTuc").Select(MapFromXElement).ToList() ?? new List<TinTucModel>();
            }
        }

        public TinTucModel? GetById(string maTT)
        {
            lock (PrivateXmlStore.Gate)
            {
                if (!File.Exists(_xmlPath)) return null;
                var doc = XDocument.Load(_xmlPath);
                var elem = doc.Root?.Elements("TinTuc")
                    .FirstOrDefault(x => string.Equals((string?)x.Element("MaTT"), maTT, StringComparison.OrdinalIgnoreCase));
                return elem != null ? MapFromXElement(elem) : null;
            }
        }

        public List<TinTucModel> GetHotNews(int count = 4)
        {
            return GetAll()
                .Where(x => !string.IsNullOrWhiteSpace(x.HinhAnh))
                .OrderByDescending(x => x.NgayDang)
                .ThenByDescending(x => x.NoiBat)
                .Take(count)
                .ToList();
        }

        public List<TinTucModel> GetLatest(int count = 6)
        {
            return GetAll()
                .OrderByDescending(x => x.NgayDang)
                .Take(count)
                .ToList();
        }

        public List<TinTucModel> SearchAndFilter(string? keyword, string? danhMuc)
        {
            var list = GetAll();

            if (!string.IsNullOrWhiteSpace(danhMuc) && danhMuc != "ALL")
            {
                list = list.Where(x => string.Equals(x.DanhMuc, danhMuc, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLowerInvariant();
                list = list.Where(x =>
                    x.TieuDe.ToLowerInvariant().Contains(kw) ||
                    x.TomTat.ToLowerInvariant().Contains(kw) ||
                    x.DanhMuc.ToLowerInvariant().Contains(kw)).ToList();
            }

            return list.OrderByDescending(x => x.NgayDang).ToList();
        }

        public void IncrementViews(string maTT)
        {
            lock (PrivateXmlStore.Gate)
            {
                if (!File.Exists(_xmlPath)) return;
                var doc = XDocument.Load(_xmlPath);
                var elem = doc.Root?.Elements("TinTuc")
                    .FirstOrDefault(x => string.Equals((string?)x.Element("MaTT"), maTT, StringComparison.OrdinalIgnoreCase));
                if (elem != null)
                {
                    var viewsElem = elem.Element("LuotXem");
                    int views = int.TryParse(viewsElem?.Value, out int v) ? v : 0;
                    if (viewsElem != null)
                    {
                        viewsElem.Value = (views + 1).ToString();
                        PrivateXmlStore.Save(doc, _xmlPath);
                    }
                }
            }
        }

        private static TinTucModel MapFromXElement(XElement el)
        {
            return new TinTucModel
            {
                MaTT = el.Element("MaTT")?.Value ?? "",
                TieuDe = el.Element("TieuDe")?.Value ?? "",
                TomTat = el.Element("TomTat")?.Value ?? "",
                NoiDung = el.Element("NoiDung")?.Value ?? "",
                HinhAnh = el.Element("HinhAnh")?.Value ?? "",
                NgayDang = DateTime.TryParse(el.Element("NgayDang")?.Value, out DateTime d) ? d : DateTime.Now,
                DanhMuc = el.Element("DanhMuc")?.Value ?? "Tin công nghệ",
                TacGia = el.Element("TacGia")?.Value ?? "Ban biên tập",
                LienKetGoc = el.Element("LienKetGoc")?.Value ?? "",
                LuotXem = int.TryParse(el.Element("LuotXem")?.Value, out int v) ? v : 0,
                NoiBat = bool.TryParse(el.Element("NoiBat")?.Value, out bool nb) && nb
            };
        }
    }
}
