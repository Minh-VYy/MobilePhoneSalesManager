using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize(Roles = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminDataController(DienThoaiXmlService phones, HoaDonXmlService invoices, XmlValidationService validation, PrivateXmlStore store) : Controller
{
    public IActionResult Index()
    {
        lock (PrivateXmlStore.Gate)
        {
            _ = store.Read("BuyerOrders.xml", "Orders", _ => true);
            return View(new List<DataFileStatus> {
                Inspect("products", "Danh mục sản phẩm", phones.GetXmlPath(), phones.GetXsdPath(), "DienThoai"),
                Inspect("invoices", "Hóa đơn tại quầy", invoices.GetXmlPath(), invoices.GetXsdPath(), "HoaDon"),
                Inspect("variants", "Phiên bản & thông số mở rộng", store.PathFor("MobileStoreDatabase.xml"), store.PathFor("MobileStoreDatabase.xsd"), "Product")
            });
        }
    }
    public IActionResult Download(string id)
    {
        var files = Resolve(id);
        if (files == null) return NotFound();
        var path = files.Value.Xml;
        lock (PrivateXmlStore.Gate)
        {
            _ = store.Read("BuyerOrders.xml", "Orders", _ => true);
            if (!System.IO.File.Exists(path)) return NotFound();
            return File(System.IO.File.ReadAllBytes(path), "application/xml", Path.GetFileName(path));
        }
    }
    public IActionResult Validation(string id)
    {
        var files = Resolve(id);
        if (files == null) return NotFound();
        lock (PrivateXmlStore.Gate) return View(validation.ValidateXml(files.Value.Xml, files.Value.Schema));
    }
    private (string Xml, string Schema)? Resolve(string id) => id switch
    {
        "products" => (phones.GetXmlPath(), phones.GetXsdPath()),
        "invoices" => (invoices.GetXmlPath(), invoices.GetXsdPath()),
        "variants" => (store.PathFor("MobileStoreDatabase.xml"), store.PathFor("MobileStoreDatabase.xsd")),
        _ => null
    };
    private DataFileStatus Inspect(string key, string name, string path, string schema, string element)
    {
        var file = new FileInfo(path);
        var result = validation.ValidateXml(path, schema);
        var count = 0;
        try { if (file.Exists) count = System.Xml.Linq.XDocument.Load(path).Root?.Descendants(element).Count() ?? 0; }
        catch (System.Xml.XmlException) { }
        return new(key, name, count, file.Exists ? file.Length : 0, file.Exists ? new DateTimeOffset(file.LastWriteTimeUtc) : null, result);
    }
}
