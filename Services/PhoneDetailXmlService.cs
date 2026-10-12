using System.Globalization;
using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public class PhoneDetailXmlService
{
    private readonly string _catalogPath;
    private readonly string _reviewPath;
    private readonly string _orderRequestPath;
    private static readonly object FileLock = new();

    public PhoneDetailXmlService(IWebHostEnvironment env)
    {
        _catalogPath = Path.Combine(env.ContentRootPath, "Data", "MobileStoreDatabase.xml");
        _reviewPath = Path.Combine(env.ContentRootPath, "Data", "ProductReviews.xml");
        _orderRequestPath = Path.Combine(env.ContentRootPath, "Data", "CustomerOrderRequests.xml");
    }

    public Dictionary<string, string> GetModelSeriesMap()
    {
        var document = XDocument.Load(_catalogPath);
        return document.Descendants("Product")
            .Where(x => x.Attribute("id") != null)
            .ToDictionary(
                x => (string)x.Attribute("id")!,
                x => (string?)x.Element("BasicInfo")?.Element("ModelSeries") ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
    }

    public PhoneDetailMetadata? GetProductMetadata(string productId)
    {
        var document = XDocument.Load(_catalogPath);
        var product = document.Descendants("Product")
            .FirstOrDefault(x => string.Equals((string?)x.Attribute("id"), productId, StringComparison.OrdinalIgnoreCase));
        return product == null ? null : MapMetadata(product);
    }

    public List<PhoneDetailMetadata> GetVariants(string productId)
    {
        var document = XDocument.Load(_catalogPath);
        var product = document.Descendants("Product").FirstOrDefault(x => (string?)x.Attribute("id") == productId);
        if (product == null) return new List<PhoneDetailMetadata>();

        var series = (string?)product.Element("BasicInfo")?.Element("ModelSeries") ?? string.Empty;
        return document.Descendants("Product")
            .Where(x => string.Equals((string?)x.Element("BasicInfo")?.Element("ModelSeries"), series, StringComparison.OrdinalIgnoreCase))
            .Select(MapMetadata)
            .OrderBy(x => x.ConditionType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ColorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Sku, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<ProductReviewModel> GetReviews(string productId)
    {
        lock (FileLock)
        {
            if (!File.Exists(_reviewPath)) return new List<ProductReviewModel>();
            return XDocument.Load(_reviewPath).Root?.Elements("Review")
                .Where(x => (string?)x.Element("ProductId") == productId)
                .Select(x => new ProductReviewModel
                {
                    ProductId = (string?)x.Element("ProductId") ?? string.Empty,
                    CustomerName = (string?)x.Element("CustomerName") ?? string.Empty,
                    Rating = int.TryParse((string?)x.Element("Rating"), out var rating) ? rating : 0,
                    Content = (string?)x.Element("Content") ?? string.Empty,
                    CreatedAt = DateTime.TryParse((string?)x.Element("CreatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdAt) ? createdAt : DateTime.MinValue
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToList() ?? new List<ProductReviewModel>();
        }
    }

    public void AddReview(ProductReviewModel review)
    {
        lock (FileLock)
        {
            var document = File.Exists(_reviewPath) ? XDocument.Load(_reviewPath) : new XDocument(new XElement("Reviews"));
            document.Root!.Add(new XElement("Review",
                new XElement("ProductId", review.ProductId),
                new XElement("CustomerName", review.CustomerName),
                new XElement("Rating", review.Rating),
                new XElement("Content", review.Content),
                new XElement("CreatedAt", review.CreatedAt.ToString("O", CultureInfo.InvariantCulture))));
            document.Save(_reviewPath);
        }
    }

    public string AddOrderRequest(CustomerOrderRequestModel request, IReadOnlyList<(DienThoaiModel Product, int Quantity)> items)
    {
        lock (FileLock)
        {
            var document = File.Exists(_orderRequestPath) ? XDocument.Load(_orderRequestPath) : new XDocument(new XElement("CustomerOrderRequests"));
            var requestId = $"YCM-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28];
            var order = new XElement("Request",
                new XAttribute("id", requestId),
                new XAttribute("createdAt", DateTime.Now.ToString("O", CultureInfo.InvariantCulture)),
                new XElement("Customer",
                    new XElement("Name", request.CustomerName),
                    new XElement("Phone", request.Phone),
                    new XElement("Address", request.Address)),
                new XElement("Items", items.Select(x => new XElement("Item",
                    new XAttribute("productId", x.Product.MaSP),
                    new XElement("Name", x.Product.TenSP),
                    new XElement("Quantity", x.Quantity),
                    new XElement("UnitPrice", x.Product.GiaBan),
                    new XElement("Subtotal", x.Product.GiaBan * x.Quantity)))),
                new XElement("Status", "PendingContact"));
            document.Root!.Add(order);
            document.Save(_orderRequestPath);
            return requestId;
        }
    }

    private static PhoneDetailMetadata MapMetadata(XElement product)
    {
        var basic = product.Element("BasicInfo");
        var condition = basic?.Element("Condition");
        var color = product.Element("ColorVariant");
        var pricing = product.Element("PricingAndStock");
        var specifications = product.Element("Specifications");
        return new PhoneDetailMetadata
        {
            ProductId = (string?)product.Attribute("id") ?? string.Empty,
            ModelSeries = (string?)basic?.Element("ModelSeries") ?? string.Empty,
            ConditionType = (string?)condition?.Attribute("type") ?? string.Empty,
            ConditionGrade = (string?)condition?.Attribute("grade") ?? string.Empty,
            ConditionDescription = condition?.Value ?? string.Empty,
            WarrantyMonths = int.TryParse((string?)basic?.Element("WarrantyMonths"), out var warranty) ? warranty : 0,
            ColorName = LocalizeColor((string?)color?.Element("ColorName") ?? string.Empty),
            ColorCode = (string?)color?.Element("ColorCode") ?? string.Empty,
            Image = (string?)color?.Element("PrimaryImage") ?? string.Empty,
            OriginalPrice = decimal.TryParse((string?)pricing?.Element("OriginalPrice"), NumberStyles.Number, CultureInfo.InvariantCulture, out var originalPrice) ? originalPrice : 0,
            Sku = (string?)pricing?.Element("SKU") ?? string.Empty,
            ScreenSize = (string?)specifications?.Element("Display")?.Element("ScreenSize") ?? string.Empty,
            Resolution = (string?)specifications?.Element("Display")?.Element("Resolution") ?? string.Empty,
            RAM = (string?)specifications?.Element("Performance")?.Element("RAM") ?? string.Empty,
            Storage = (string?)specifications?.Element("Performance")?.Element("Storage") ?? string.Empty,
            Battery = (string?)specifications?.Element("BatteryAndCharging")?.Element("CapacityMah") ?? string.Empty,
            Camera = (string?)specifications?.Element("Camera")?.Element("MainSensor") ?? string.Empty,
            ChargingTechnology = (string?)specifications?.Element("BatteryAndCharging")?.Element("ChargingTechnology") ?? string.Empty,
            Ports = (string?)specifications?.Element("DesignAndConnectivity")?.Element("Ports") ?? string.Empty,
            Weight = (string?)specifications?.Element("DesignAndConnectivity")?.Element("WeightGrams") ?? string.Empty
        };
    }

    private static string LocalizeColor(string color) => color switch
    {
        "Natural Titanium" => "Titan tự nhiên",
        "Blue Titanium" => "Titan xanh",
        "Deep Purple" => "Tím đậm",
        "Titanium Gray" => "Xám Titan",
        "Black Leather" => "Đen",
        "Sepia Brown" => "Nâu",
        "Bay Blue" => "Xanh biển",
        "Phantom Black" => "Đen Phantom",
        _ => color
    };
}
