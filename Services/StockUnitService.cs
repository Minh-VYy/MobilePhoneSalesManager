using System.Text.RegularExpressions;
using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public sealed class StockUnitService(PrivateXmlStore store, DienThoaiXmlService phones)
{
    public List<StockUnit> GetAll() => store.Read("StockUnits.xml", "StockUnits", root => root.Elements("Unit").Select(Map).OrderByDescending(x => x.RegisteredAt).ToList());
    public StockUnit? Lookup(string code) => store.Read("StockUnits.xml", "StockUnits", root => Find(root, Normalize(code)) is XElement unit ? Map(unit) : null);

    public void Register(RegisterStockUnitInput input, string employee)
    {
        store.Update("StockUnits.xml", "StockUnits", root =>
        {
            var code = Normalize(input.Code);
            var alternate = string.IsNullOrWhiteSpace(input.AlternateCode) ? "" : Normalize(input.AlternateCode);
            if (code == alternate || Find(root, code) != null || (alternate.Length > 0 && Find(root, alternate) != null))
                throw new InvalidOperationException("IMEI/serial này đã được đăng ký. Mỗi máy và mã phụ chỉ được ghi nhận một lần.");
            var phone = phones.GetById(input.ProductId) ?? throw new InvalidOperationException("Phiên bản sản phẩm không còn tồn tại.");
            var registered = root.Elements("Unit").Count(x => (string?)x.Attribute("productId") == phone.MaSP && (string?)x.Attribute("status") == "Available");
            if (registered >= phone.SoLuongTon) throw new InvalidOperationException("Số máy đã gắn mã đạt số lượng tồn kho khả dụng. Hãy đối chiếu tồn kho trước khi đăng ký thêm.");
            root.Add(new XElement("Unit", new XAttribute("code", code), new XAttribute("alternate", alternate), new XAttribute("productId", phone.MaSP),
                new XAttribute("status", "Available"), new XAttribute("registeredAt", DateTimeOffset.UtcNow.ToString("O")),
                new XElement("ProductName", phone.TenSP), new XElement("RegisteredBy", employee)));
            return true;
        });
    }

    public static string Normalize(string code)
    {
        var value = (code ?? "").Trim().ToUpperInvariant();
        if (!Regex.IsMatch(value, @"\A[A-Z0-9][A-Z0-9._/-]{2,79}\z")) throw new InvalidOperationException("Mã máy chưa hợp lệ. Quét IMEI/serial gồm 3–80 ký tự.");
        return value;
    }

    public void Unregister(string code)
    {
        store.Update("StockUnits.xml", "StockUnits", root =>
        {
            var unit = Find(root, Normalize(code)) ?? throw new InvalidOperationException("Mã máy không còn tồn tại.");
            if ((string?)unit.Attribute("status") != "Available" || !string.IsNullOrEmpty((string?)unit.Element("Reference")))
                throw new InvalidOperationException("Chỉ được gỡ mã chưa bán và chưa giữ cho đơn hàng.");
            unit.Remove();
            return true;
        });
    }

    public static XElement? Find(XElement root, string code) => root.Elements("Unit").FirstOrDefault(x =>
        string.Equals((string?)x.Attribute("code"), code, StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrEmpty((string?)x.Attribute("alternate")) && string.Equals((string?)x.Attribute("alternate"), code, StringComparison.OrdinalIgnoreCase)));

    public static void ReserveForOrder(XElement root, string productId, int quantity, string orderId)
    {
        // Existing untracked stock remains usable; reserve every registered unit involved first.
        foreach (var unit in root.Elements("Unit").Where(x => (string?)x.Attribute("productId") == productId && (string?)x.Attribute("status") == "Available").Take(quantity))
        {
            unit.SetAttributeValue("status", "Reserved"); unit.SetElementValue("Reference", orderId); unit.SetElementValue("Channel", "Online");
        }
    }
    public static void UpdateOrderUnits(XElement root, BuyerOrder order, string next)
    {
        foreach (var unit in root.Elements("Unit").Where(x => (string?)x.Element("Reference") == order.Id && (string?)x.Attribute("status") == "Reserved"))
        {
            if (next == "Cancelled")
            {
                unit.SetAttributeValue("status", "Available"); unit.Element("Reference")?.Remove(); unit.Element("Channel")?.Remove();
            }
            else if (next == "Delivered")
            {
                var item = order.Items.First(x => x.ProductId == (string?)unit.Attribute("productId"));
                MarkSold(unit, order.Id, "Online", item.UnitPrice, "Đơn trực tuyến");
            }
        }
    }
    public static void MarkSold(XElement unit, string reference, string channel, decimal price, string employee)
    {
        unit.SetAttributeValue("status", "Sold"); unit.SetElementValue("Reference", reference); unit.SetElementValue("Channel", channel);
        unit.SetElementValue("SoldAt", DateTimeOffset.UtcNow.ToString("O")); unit.SetElementValue("SalePrice", price); unit.SetElementValue("Employee", employee);
    }
    private static StockUnit Map(XElement unit) => new((string)unit.Attribute("code")!, (string?)unit.Attribute("alternate"),
        (string)unit.Attribute("productId")!, (string?)unit.Element("ProductName") ?? "", (string?)unit.Attribute("status") ?? "Available",
        DateTimeOffset.Parse((string)unit.Attribute("registeredAt")!), (string?)unit.Element("Reference") ?? "", (string?)unit.Element("Channel") ?? "",
        DateTimeOffset.TryParse((string?)unit.Element("SoldAt"), out var at) ? at : null, (decimal?)unit.Element("SalePrice") ?? 0, (string?)unit.Element("Employee") ?? "");
}
