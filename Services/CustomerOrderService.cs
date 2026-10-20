using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public sealed class CustomerOrderService(PrivateXmlStore store, DienThoaiXmlService phones, PhoneDetailXmlService details)
{
    public List<BuyerOrder> GetAll(string? accountId = null) => store.Read("BuyerOrders.xml", "Orders", root => root.Elements("Order")
        .Where(x => accountId == null || (string?)x.Attribute("accountId") == accountId).Select(Map).OrderByDescending(x => x.CreatedAt).ToList());
    public BuyerOrder? Get(string id) => store.Read("BuyerOrders.xml", "Orders", root => root.Elements("Order").Where(x => (string?)x.Attribute("id") == id).Select(Map).FirstOrDefault());

    public BuyerOrder Create(string accountId, CustomerOrderRequestModel input)
    {
        if (!Guid.TryParse(input.CheckoutToken, out _) || input.Items.Count is < 1 or > 20 || input.Items.Any(x => x.Quantity is < 1 or > 20))
            throw new InvalidOperationException("Giỏ hàng chưa hợp lệ. Mỗi đơn có tối đa 20 mẫu, mỗi mẫu tối đa 20 máy.");
        lock (PrivateXmlStore.Gate)
        {
            return store.Update("BuyerOrders.xml", "Orders", root =>
            {
                var existing = root.Elements("Order").FirstOrDefault(x => (string?)x.Attribute("accountId") == accountId && (string?)x.Attribute("checkoutToken") == input.CheckoutToken);
                if (existing != null) return Map(existing);
                var products = phones.GetAll().ToDictionary(x => x.MaSP, StringComparer.OrdinalIgnoreCase);
                var items = input.Items.GroupBy(x => x.ProductId, StringComparer.OrdinalIgnoreCase).Select(group =>
                {
                    var quantity = group.Sum(x => x.Quantity);
                    if (!products.TryGetValue(group.Key ?? "", out var phone) || quantity > Math.Min(20, phone.SoLuongTon))
                        throw new InvalidOperationException("Một sản phẩm đã hết hàng hoặc không đủ số lượng. Hãy cập nhật lại giỏ hàng.");
                    var meta = details.GetProductMetadata(phone.MaSP);
                    return new XElement("Item", new XAttribute("productId", phone.MaSP), new XElement("Name", phone.TenSP),
                        new XElement("Image", phone.HinhAnh), new XElement("Options", $"{meta?.ColorName} · {phone.ThongSo.BoNhoTrong} · Bảo hành {meta?.WarrantyMonths ?? 0} tháng"),
                        new XElement("UnitPrice", phone.GiaBan), new XElement("Quantity", quantity));
                }).ToList();
                var now = DateTimeOffset.UtcNow;
                var order = new XElement("Order", new XAttribute("id", $"DH-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}"),
                    new XAttribute("accountId", accountId), new XAttribute("checkoutToken", input.CheckoutToken), new XAttribute("createdAt", now.ToString("O")),
                    new XElement("CustomerName", input.CustomerName.Trim()), new XElement("Phone", input.Phone), new XElement("Address", input.Address.Trim()),
                    new XElement("Note", input.Note?.Trim() ?? ""), new XElement("Status", "Pending"), new XElement("Items", items),
                    new XElement("History", new XElement("Event", new XAttribute("status", "Pending"), new XAttribute("at", now.ToString("O")))));
                root.Add(order);
                return Map(order);
            });
        }
    }

    public void ChangeStatus(string id, string next, string? ownerId = null)
    {
        store.UpdateCommerce((root, products, units) =>
        {
            var element = root.Elements("Order").FirstOrDefault(x => (string?)x.Attribute("id") == id);
            if (element == null || (ownerId != null && (string?)element.Attribute("accountId") != ownerId)) throw new InvalidOperationException("Không tìm thấy đơn hàng.");
            var order = Map(element);
            if (ownerId != null && (order.Status != "Pending" || next != "Cancelled")) throw new InvalidOperationException("Chỉ có thể hủy đơn đang chờ xác nhận. Hãy nhắn hỗ trợ nếu cần thay đổi đơn đã xác nhận.");
            if (!BuyerOrder.NextStatuses(order.Status).Contains(next)) throw new InvalidOperationException("Trạng thái đơn hàng đã thay đổi. Vui lòng tải lại để xem thông tin mới.");
            if (next == "Confirmed" || (order.Status == "Confirmed" && next == "Cancelled"))
            {
                foreach (var item in order.Items)
                {
                    var product = products.Elements("DienThoai").FirstOrDefault(x => (string?)x.Element("MaSP") == item.ProductId);
                    if (product == null) throw new InvalidOperationException("Sản phẩm trong đơn không còn tồn tại.");
                    var stock = (int?)product.Element("SoLuongTon") ?? 0;
                    var remaining = checked(stock - (next == "Confirmed" ? item.Quantity : -item.Quantity));
                    if (remaining < 0) throw new InvalidOperationException("Kho không đủ hàng để xác nhận đơn. Hãy kiểm tra lại tồn kho.");
                    product.SetElementValue("SoLuongTon", remaining);
                    if (next == "Confirmed") StockUnitService.ReserveForOrder(units, item.ProductId, item.Quantity, order.Id);
                }
            }
            StockUnitService.UpdateOrderUnits(units, order, next);
            element.SetElementValue("Status", next);
            element.Element("History")!.Add(new XElement("Event", new XAttribute("status", next), new XAttribute("at", DateTimeOffset.UtcNow.ToString("O"))));
        });
    }

    private static BuyerOrder Map(XElement x) => new()
    {
        Id = (string)x.Attribute("id")!,
        AccountId = (string)x.Attribute("accountId")!,
        CreatedAt = DateTimeOffset.Parse((string)x.Attribute("createdAt")!),
        Status = (string?)x.Element("Status") ?? "Pending",
        CustomerName = (string?)x.Element("CustomerName") ?? "",
        Phone = (string?)x.Element("Phone") ?? "",
        Address = (string?)x.Element("Address") ?? "",
        Note = (string?)x.Element("Note") ?? "",
        Items = x.Element("Items")!.Elements("Item").Select(i => new BuyerOrderItem
        {
            ProductId = (string)i.Attribute("productId")!,
            Name = (string)i.Element("Name")!,
            Image = (string?)i.Element("Image") ?? "",
            Options = (string?)i.Element("Options") ?? "",
            UnitPrice = (decimal)i.Element("UnitPrice")!,
            Quantity = (int)i.Element("Quantity")!
        }).ToList(),
        History = x.Element("History")!.Elements("Event").Select(e => new OrderEvent((string)e.Attribute("status")!, DateTimeOffset.Parse((string)e.Attribute("at")!))).ToList()
    };
}
