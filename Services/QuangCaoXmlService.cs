using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public class QuangCaoXmlService
{
    private readonly string _xmlPath;
    private static readonly object FileLock = new();

    public QuangCaoXmlService(IWebHostEnvironment env)
    {
        _xmlPath = Path.Combine(env.ContentRootPath, "Data", "QuangCao.xml");
    }

    public List<QuangCaoModel> GetAll()
    {
        lock (FileLock)
        {
            if (!File.Exists(_xmlPath)) return new List<QuangCaoModel>();
            var doc = XDocument.Load(_xmlPath);
            return doc.Root?.Elements("QuangCao").Select(ReadAd).OrderBy(x => x.ThuTu).ToList() ?? new List<QuangCaoModel>();
        }
    }

    public List<QuangCaoModel> GetActive() => GetAll().Where(x => x.DangHienThi).ToList();

    public void Add(QuangCaoModel ad)
    {
        lock (FileLock)
        {
            var ads = GetAll();
            ad.MaQC = Guid.NewGuid().ToString("N");
            ads.Add(ad);
            SaveAll(ads);
        }
    }

    public bool Update(QuangCaoModel ad)
    {
        lock (FileLock)
        {
            var ads = GetAll();
            var index = ads.FindIndex(x => x.MaQC == ad.MaQC);
            if (index < 0) return false;
            ads[index] = ad;
            SaveAll(ads);
            return true;
        }
    }

    public bool SetVisibility(string id, bool enabled)
    {
        lock (FileLock)
        {
            var ads = GetAll();
            var ad = ads.FirstOrDefault(x => x.MaQC == id);
            if (ad == null) return false;
            ad.DangHienThi = enabled;
            SaveAll(ads);
            return true;
        }
    }

    public bool Delete(string id)
    {
        lock (FileLock)
        {
            var ads = GetAll();
            var removed = ads.RemoveAll(x => x.MaQC == id) > 0;
            if (removed) SaveAll(ads);
            return removed;
        }
    }

    private void SaveAll(IEnumerable<QuangCaoModel> ads)
    {
        var root = new XElement("DanhSachQuangCao", ads.OrderBy(x => x.ThuTu).Select(x => new XElement("QuangCao",
            new XElement("MaQC", x.MaQC),
            new XElement("TieuDe", x.TieuDe),
            new XElement("MoTa", x.MoTa),
            new XElement("HinhAnh", x.HinhAnh),
            new XElement("ThuTu", x.ThuTu),
            new XElement("ThoiGianChuyenGiay", x.ThoiGianChuyenGiay),
            new XElement("DangHienThi", x.DangHienThi))));
        Directory.CreateDirectory(Path.GetDirectoryName(_xmlPath)!);
        var tempPath = _xmlPath + ".tmp";
        new XDocument(new XDeclaration("1.0", "utf-8", null), root).Save(tempPath);
        File.Move(tempPath, _xmlPath, true);
    }

    private static QuangCaoModel ReadAd(XElement e) => new()
    {
        MaQC = (string?)e.Element("MaQC") ?? Guid.NewGuid().ToString("N"),
        TieuDe = (string?)e.Element("TieuDe") ?? string.Empty,
        MoTa = (string?)e.Element("MoTa") ?? string.Empty,
        HinhAnh = (string?)e.Element("HinhAnh") ?? string.Empty,
        ThuTu = (int?)e.Element("ThuTu") ?? 1,
        ThoiGianChuyenGiay = (int?)e.Element("ThoiGianChuyenGiay") ?? 6,
        DangHienThi = (bool?)e.Element("DangHienThi") ?? true
    };
}
