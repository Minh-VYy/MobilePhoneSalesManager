using System.Xml.Linq;

namespace QuanLyBanDienThoai.Services;

// All private XML writes share one lock and replace the destination atomically.
public sealed class PrivateXmlStore(IWebHostEnvironment environment)
{
    public static object Gate { get; } = new();
    public string PathFor(string name) => Path.Combine(environment.ContentRootPath, "Data", name);

    public T Read<T>(string name, string root, Func<XElement, T> read)
    {
        lock (Gate) { RecoverCommerce(); return read(Load(name, root)); }
    }

    public T Update<T>(string name, string root, Func<XElement, T> update)
    {
        lock (Gate)
        {
            RecoverCommerce();
            var document = Load(name, root);
            var result = update(document);
            var path = PathFor(name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Save(new XDocument(document), path);
            return result;
        }
    }

    // A durable journal makes inventory + order status replayable after an interrupted write.
    public void UpdateCommerce(Action<XElement, XElement, XElement> update)
    {
        lock (Gate)
        {
            RecoverCommerce();
            var orders = Load("BuyerOrders.xml", "Orders");
            var products = Load("DienThoai.xml", "DanhSachDienThoai");
            var units = Load("StockUnits.xml", "StockUnits");
            update(orders, products, units);
            Save(new XDocument(new XElement("Transaction", new XElement("Orders", orders), new XElement("Products", products), new XElement("Units", units))), PathFor("CommerceTransaction.xml"));
            RecoverCommerce();
        }
    }

    public T UpdatePointOfSale<T>(Func<XElement, XElement, XElement, T> update)
    {
        lock (Gate)
        {
            RecoverCommerce();
            var invoices = Load("HoaDon.xml", "DanhSachHoaDon");
            var products = Load("DienThoai.xml", "DanhSachDienThoai");
            var units = Load("StockUnits.xml", "StockUnits");
            var result = update(invoices, products, units);
            Save(new XDocument(new XElement("Transaction", new XElement("Invoices", invoices), new XElement("Products", products), new XElement("Units", units))), PathFor("CommerceTransaction.xml"));
            RecoverCommerce();
            return result;
        }
    }

    private void RecoverCommerce()
    {
        var journal = PathFor("CommerceTransaction.xml");
        if (!File.Exists(journal)) return;
        var document = XDocument.Load(journal).Root!;
        Save(new XDocument(new XElement(document.Element("Products")!.Elements().Single())), PathFor("DienThoai.xml"));
        if (document.Element("Orders") is XElement orders) Save(new XDocument(new XElement(orders.Elements().Single())), PathFor("BuyerOrders.xml"));
        if (document.Element("Invoices") is XElement invoices) Save(new XDocument(new XElement(invoices.Elements().Single())), PathFor("HoaDon.xml"));
        if (document.Element("Units") is XElement units) Save(new XDocument(new XElement(units.Elements().Single())), PathFor("StockUnits.xml"));
        File.Delete(journal);
    }

    public static void Save(XDocument document, string path)
    {
        var temporary = path + ".tmp";
        try { document.Save(temporary); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private XElement Load(string name, string root) => File.Exists(PathFor(name))
        ? XDocument.Load(PathFor(name)).Root ?? throw new InvalidDataException($"Invalid {name}")
        : new XElement(root);
}
