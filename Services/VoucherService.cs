using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public sealed class VoucherService(PrivateXmlStore store, DienThoaiXmlService phones, XmlValidationService validation)
{
    public List<VoucherDefinition> GetAll()
    {
        lock (PrivateXmlStore.Gate)
        {
            var uses = ActiveUses();
            return store.Read("Vouchers.xml", "Vouchers", root => root.Elements("Voucher").Select(Map)
                .Select(voucher => { voucher.UsedCount = uses.Count(x => string.Equals((string?)x.Element("VoucherCode"), voucher.Code, StringComparison.OrdinalIgnoreCase)); return voucher; })
                .OrderByDescending(x => x.Available).ThenBy(x => x.ExpiryDate).ToList());
        }
    }
    public VoucherDefinition? Get(string code) => GetAll().FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
    public List<VoucherDefinition> PublicCards(string? productId = null) => GetAll().Where(x => x.Public && x.Available && (productId == null || x.AppliesTo(productId))).ToList();

    public void Save(VoucherInput input, bool create)
    {
        input.Code = Normalize(input.Code);
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 120 || input.Description?.Length > 400 || input.Benefits?.Length > 1200)
            throw new InvalidOperationException("Kiểm tra tiêu đề, mô tả và quyền lợi của thẻ giảm giá.");
        if (input.DiscountType is not ("FixedAmount" or "Percentage") || input.DiscountValue <= 0 || input.DiscountValue > 1000000000 ||
            (input.DiscountType == "Percentage" && input.DiscountValue > 100) || (input.DiscountType == "FixedAmount" && decimal.Truncate(input.DiscountValue) != input.DiscountValue))
            throw new InvalidOperationException("Số tiền giảm phải là số nguyên dương; phần trăm giảm phải lớn hơn 0 và không vượt quá 100%.");
        if (input.StartDate == null || input.ExpiryDate == null || input.StartDate > input.ExpiryDate || input.MinOrderAmount < 0 || input.MinOrderAmount > 1000000000000 ||
            input.MaxDiscount is <= 0 or > 1000000000 || input.UsageLimit is < 1 or > 1000000 || input.PerAccountLimit < 1 || input.PerAccountLimit > input.UsageLimit)
            throw new InvalidOperationException("Kiểm tra thời hạn, số tiền tối thiểu, mức giảm tối đa và giới hạn lượt dùng.");
        if (input.ProductIds.Count > 500 || input.ProductIds.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException("Chọn tối đa 500 phiên bản hợp lệ.");
        lock (PrivateXmlStore.Gate)
        {
            var products = phones.GetAll().ToDictionary(x => x.MaSP, StringComparer.OrdinalIgnoreCase);
            input.ProductIds = input.ProductIds.Distinct(StringComparer.OrdinalIgnoreCase).Select(id => products.TryGetValue(id, out var product) ? product.MaSP
                : throw new InvalidOperationException($"Sản phẩm {id} không còn trong danh mục.")).ToList();
            if (ActiveUses().Count(x => string.Equals((string?)x.Element("VoucherCode"), input.Code, StringComparison.OrdinalIgnoreCase)) > input.UsageLimit)
                throw new InvalidOperationException("Giới hạn tổng lượt không được thấp hơn số lượt đang sử dụng.");
            store.Update("Vouchers.xml", "Vouchers", root =>
            {
                var existing = root.Elements("Voucher").FirstOrDefault(x => string.Equals((string?)x.Attribute("code"), input.Code, StringComparison.OrdinalIgnoreCase));
                if (create && existing != null) throw new InvalidOperationException("Mã giảm giá đã tồn tại.");
                if (!create && existing == null) throw new InvalidOperationException("Mã giảm giá không còn tồn tại.");
                if (existing == null) root.Add(Element(input)); else existing.ReplaceWith(Element(input));
                Validate(root); return true;
            });
        }
    }
    public void SetEnabled(string code, bool enabled) => store.Update("Vouchers.xml", "Vouchers", root =>
    {
        var item = root.Elements("Voucher").FirstOrDefault(x => string.Equals((string?)x.Attribute("code"), code, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Không tìm thấy mã giảm giá.");
        item.SetElementValue("Status", enabled ? "unused" : "disabled"); Validate(root); return true;
    });

    // Called under the shared commerce lock by both preview and order creation.
    public CheckoutQuote Apply(string accountId, string? code, List<BuyerOrderItem> items)
    {
        lock (PrivateXmlStore.Gate)
        {
            foreach (var item in items) item.DiscountAmount = 0;
            var subtotal = items.Sum(x => x.Subtotal);
            if (string.IsNullOrWhiteSpace(code)) return new(subtotal, 0, subtotal, "", "", items);
            code = Normalize(code);
            var voucher = Get(code) ?? throw new InvalidOperationException("Mã giảm giá không tồn tại.");
            if (!voucher.Available) throw new InvalidOperationException($"Mã {code}: {voucher.StatusLabel.ToLowerInvariant()}.");
            if (ActiveUses().Count(x => (string?)x.Attribute("accountId") == accountId && string.Equals((string?)x.Element("VoucherCode"), code, StringComparison.OrdinalIgnoreCase)) >= voucher.PerAccountLimit)
                throw new InvalidOperationException("Bạn đã đạt giới hạn sử dụng mã này. Đơn đang chờ xử lý cũng giữ một lượt dùng.");
            var eligible = items.Where(x => voucher.AppliesTo(x.ProductId)).OrderBy(x => x.ProductId, StringComparer.OrdinalIgnoreCase).ToList();
            var eligibleSubtotal = eligible.Sum(x => x.Subtotal);
            if (eligibleSubtotal <= 0) throw new InvalidOperationException("Mã không áp dụng cho sản phẩm trong giỏ hàng.");
            if (eligibleSubtotal < voucher.MinOrderAmount) throw new InvalidOperationException($"Giá trị sản phẩm được áp dụng cần từ {voucher.MinOrderAmount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} ₫.");
            var discount = voucher.DiscountType == "Percentage" ? decimal.Floor(eligibleSubtotal * voucher.DiscountValue / 100) : voucher.DiscountValue;
            discount = decimal.Floor(Math.Min(eligibleSubtotal, Math.Min(discount, voucher.MaxDiscount ?? decimal.MaxValue)));
            if (discount <= 0) throw new InvalidOperationException("Giỏ hàng chưa phát sinh số tiền giảm hợp lệ với mã này.");
            foreach (var item in eligible) item.DiscountAmount = decimal.Floor(discount * (item.Subtotal / eligibleSubtotal));
            var remaining = discount - eligible.Sum(x => x.DiscountAmount);
            foreach (var item in eligible)
            {
                var extra = Math.Min(remaining, item.Subtotal - item.DiscountAmount);
                item.DiscountAmount += extra; remaining -= extra;
                if (remaining == 0) break;
            }
            return new(subtotal, discount, subtotal - discount, voucher.Code, voucher.Title, items);
        }
    }
    private List<XElement> ActiveUses() => store.Read("BuyerOrders.xml", "Orders", root => root.Elements("Order").Where(x => (string?)x.Element("Status") != "Cancelled" && !string.IsNullOrEmpty((string?)x.Element("VoucherCode"))).Select(x => new XElement(x)).ToList());
    public static string Normalize(string code)
    {
        var result = (code ?? "").Trim().ToUpperInvariant();
        if (!Regex.IsMatch(result, @"\A[A-Z0-9][A-Z0-9_-]{2,31}\z")) throw new InvalidOperationException("Mã gồm 3–32 chữ không dấu, số, gạch dưới hoặc gạch ngang.");
        return result;
    }
    private void Validate(XElement root)
    {
        var result = validation.ValidateXmlContent(root.ToString(), store.PathFor("Vouchers.xsd"));
        if (!result.IsValid) throw new InvalidOperationException("Dữ liệu mã giảm giá không đáp ứng lược đồ XSD. Chưa lưu thay đổi.");
    }
    private static XElement Element(VoucherInput input) => new("Voucher", new XAttribute("code", input.Code), new XElement("Title", input.Title.Trim()),
        new XElement("DiscountType", input.DiscountType), new XElement("DiscountValue", input.DiscountValue), new XElement("MinOrderAmount", input.MinOrderAmount),
        new XElement("ExpiryDate", input.ExpiryDate!.Value.ToString("yyyy-MM-dd")), new XElement("Status", input.Enabled ? "unused" : "disabled"),
        new XElement("StartDate", input.StartDate!.Value.ToString("yyyy-MM-dd")), input.MaxDiscount == null ? null : new XElement("MaxDiscount", input.MaxDiscount.Value),
        new XElement("UsageLimit", input.UsageLimit), new XElement("PerAccountLimit", input.PerAccountLimit), new XElement("Public", input.Public),
        new XElement("Description", input.Description?.Trim() ?? ""), new XElement("Benefits", input.Benefits?.Trim() ?? ""), new XElement("ProductIds", input.ProductIds.Select(id => new XElement("Id", id))));
    private static VoucherDefinition Map(XElement x) => new()
    {
        Code = (string)x.Attribute("code")!, Title = (string?)x.Element("Title") ?? "", DiscountType = (string?)x.Element("DiscountType") ?? "FixedAmount",
        DiscountValue = (decimal?)x.Element("DiscountValue") ?? 0, MinOrderAmount = (decimal?)x.Element("MinOrderAmount") ?? 0, MaxDiscount = (decimal?)x.Element("MaxDiscount"),
        StartDate = DateOnly.TryParseExact((string?)x.Element("StartDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ? start : DateOnly.MinValue,
        ExpiryDate = DateOnly.TryParseExact((string?)x.Element("ExpiryDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) ? end : DateOnly.MinValue,
        Enabled = (string?)x.Element("Status") == "unused", Public = (bool?)x.Element("Public") ?? false, UsageLimit = (int?)x.Element("UsageLimit") ?? 1,
        PerAccountLimit = (int?)x.Element("PerAccountLimit") ?? 1, Description = (string?)x.Element("Description"), Benefits = (string?)x.Element("Benefits"),
        ProductIds = x.Element("ProductIds")?.Elements("Id").Select(id => id.Value).ToList() ?? []
    };
}
