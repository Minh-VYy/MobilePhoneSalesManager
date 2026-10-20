using System.Globalization;
using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public class PhoneDetailXmlService
{
    private readonly string _catalogPath;
    private readonly string _reviewPath;
    private readonly XmlValidationService _validation;
    private static readonly object FileLock = new();

    public PhoneDetailXmlService(IWebHostEnvironment env, XmlValidationService validation)
    {
        _catalogPath = Path.Combine(env.ContentRootPath, "Data", "MobileStoreDatabase.xml");
        _reviewPath = Path.Combine(env.ContentRootPath, "Data", "ProductReviews.xml");
        _validation = validation;
    }

    public Dictionary<string, string> GetModelSeriesMap()
    {
        var document = ReadCatalog();
        return document.Descendants("Product")
            .Where(x => x.Attribute("id") != null)
            .ToDictionary(
                x => (string)x.Attribute("id")!,
                x => (string?)x.Element("BasicInfo")?.Element("ModelSeries") ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
    }

    public PhoneDetailMetadata? GetProductMetadata(string productId)
    {
        var document = ReadCatalog();
        var product = document.Descendants("Product")
            .FirstOrDefault(x => string.Equals((string?)x.Attribute("id"), productId, StringComparison.OrdinalIgnoreCase));
        return product == null ? null : MapMetadata(product);
    }

    public List<PhoneDetailMetadata> GetVariants(string productId)
    {
        var document = ReadCatalog();
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
            PrivateXmlStore.Save(document, _reviewPath);
        }
    }

    private XDocument ReadCatalog()
    {
        lock (PrivateXmlStore.Gate) return XDocument.Load(_catalogPath);
    }

    public (bool Success, string Message) SaveVariant(DienThoaiModel phone, AdminVariantInput input)
    {
        lock (PrivateXmlStore.Gate)
        {
            var document = ReadCatalog();
            var root = document.Root!;
            var categoryId = phone.MaHang == "APPLE" ? "CAT-IPHONE" : "CAT-" + phone.MaHang;
            var categories = root.Element("Categories")!;
            if (!categories.Elements("Category").Any(x => (string?)x.Attribute("id") == categoryId))
                categories.Add(new XElement("Category", new XAttribute("id", categoryId), new XAttribute("name", phone.TenHang), new XAttribute("slug", phone.MaHang.ToLowerInvariant())));
            var product = root.Element("Products")!.Elements("Product").FirstOrDefault(x => (string?)x.Attribute("id") == phone.MaSP);
            if (product == null)
            {
                product = new XElement("Product", new XAttribute("id", phone.MaSP), new XAttribute("categoryId", categoryId),
                    new XElement("BasicInfo", new XElement("Name"), new XElement("Brand"), new XElement("ModelSeries"), new XElement("Condition"), new XElement("WarrantyMonths")),
                    new XElement("ColorVariant", new XElement("ColorName"), new XElement("ColorCode"), new XElement("PrimaryImage"), new XElement("Gallery")),
                    new XElement("PricingAndStock", new XElement("OriginalPrice"), new XElement("SellingPrice"), new XElement("StockQuantity"), new XElement("SKU"), new XElement("IsFeatured", false)),
                    new XElement("Specifications"));
                root.Element("Products")!.Add(product);
            }
            var basic = product.Element("BasicInfo")!;
            basic.SetElementValue("Name", phone.TenSP); basic.SetElementValue("Brand", phone.TenHang); basic.SetElementValue("ModelSeries", input.ModelSeries.Trim());
            basic.Element("Condition")!.ReplaceWith(new XElement("Condition", new XAttribute("type", input.ConditionType), new XAttribute("grade", input.ConditionGrade.Trim()), input.ConditionDescription ?? ""));
            basic.SetElementValue("WarrantyMonths", input.WarrantyMonths);
            var color = product.Element("ColorVariant")!;
            var image = string.IsNullOrWhiteSpace(input.ImageUrl) ? phone.HinhAnh : input.ImageUrl.Trim();
            color.SetElementValue("ColorName", input.ColorName.Trim()); color.SetElementValue("ColorCode", input.ColorCode); color.SetElementValue("PrimaryImage", image);
            if (!color.Element("Gallery")!.Elements("Image").Any()) color.Element("Gallery")!.Add(new XElement("Image", new XAttribute("url", image), new XAttribute("caption", phone.TenSP)));
            var pricing = product.Element("PricingAndStock")!;
            pricing.SetElementValue("OriginalPrice", input.OriginalPrice); pricing.SetElementValue("SellingPrice", phone.GiaBan); pricing.SetElementValue("StockQuantity", phone.SoLuongTon); pricing.SetElementValue("SKU", string.IsNullOrWhiteSpace(input.Sku) ? phone.MaSP : input.Sku.Trim());
            var specs = product.Element("Specifications")!;
            void SetSpec(string group, string key, string value)
            {
                var section = specs.Element(group);
                if (section == null) { section = new XElement(group); specs.Add(section); }
                section.SetElementValue(key, value);
            }
            SetSpec("Display", "ScreenSize", phone.ThongSo.ManHinh); SetSpec("Display", "Resolution", input.Resolution ?? "");
            SetSpec("Performance", "Storage", input.StorageGB.ToString(CultureInfo.InvariantCulture)); SetSpec("Performance", "RAM", phone.ThongSo.RAM); SetSpec("Performance", "Chipset", phone.ThongSo.Chip);
            SetSpec("Camera", "MainSensor", input.Camera ?? ""); SetSpec("BatteryAndCharging", "CapacityMah", phone.ThongSo.Pin); SetSpec("BatteryAndCharging", "ChargingTechnology", input.ChargingTechnology ?? "");
            SetSpec("DesignAndConnectivity", "Ports", input.Ports ?? ""); SetSpec("DesignAndConnectivity", "WeightGrams", input.Weight ?? "");
            var validation = _validation.ValidateXmlContent(document.ToString(), Path.ChangeExtension(_catalogPath, ".xsd"));
            if (!validation.IsValid) return (false, string.Join(" ", validation.Errors.Take(3).Select(x => x.Message)));
            PrivateXmlStore.Save(document, _catalogPath);
            return (true, "Đã cập nhật phiên bản, màu sắc, dung lượng và bảo hành.");
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
