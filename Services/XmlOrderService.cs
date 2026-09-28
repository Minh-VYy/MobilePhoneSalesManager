using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace QuanLyBanDienThoai.Services
{
    public class XmlOrderService
    {
        private readonly string _envPath;

        public XmlOrderService(IWebHostEnvironment env)
        {
            _envPath = Path.Combine(env.ContentRootPath, "Data");
        }

        // 1. Kiểm tra voucher hợp lệ qua XPath
        public (bool IsValid, decimal Discount, string Message) ValidateVoucher(string code, decimal orderSubtotal)
        {
            string voucherFile = Path.Combine(_envPath, "Vouchers.xml");
            if (!File.Exists(voucherFile)) return (false, 0, "Không tìm thấy dữ liệu voucher.");

            var doc = new XPathDocument(voucherFile);
            var nav = doc.CreateNavigator();

            string query = $"/Vouchers/Voucher[@code='{code}' and Status='unused']";
            var node = nav.SelectSingleNode(query);

            if (node == null) return (false, 0, "Mã giảm giá không tồn tại hoặc đã qua sử dụng.");

            decimal minOrder = decimal.Parse(node.SelectSingleNode("MinOrderAmount")?.Value ?? "0");
            DateTime expiry = DateTime.Parse(node.SelectSingleNode("ExpiryDate")?.Value ?? "1970-01-01");

            if (DateTime.Now > expiry) return (false, 0, "Mã giảm giá đã hết hạn.");
            if (orderSubtotal < minOrder) return (false, 0, $"Đơn hàng tối thiểu phải từ {minOrder:N0} đ.");

            decimal discount = decimal.Parse(node.SelectSingleNode("DiscountValue")?.Value ?? "0");
            return (true, discount, "Áp dụng voucher thành công!");
        }

        // 2. Validate XML bằng XSD
        public bool ValidateWithXsd(XDocument xmlDoc, string xsdFileName, out List<string> errors)
        {
            var errorList = new List<string>();
            var schemas = new XmlSchemaSet();
            schemas.Add("", Path.Combine(_envPath, xsdFileName));

            bool isValid = true;
            xmlDoc.Validate(schemas, (o, e) => {
                errorList.Add(e.Message);
                isValid = false;
            });
            errors = errorList;
            return isValid;
        }

        // 3. Kết xuất hóa đơn HTML bằng XSLT
        public string GenerateInvoiceHtml(string orderId)
        {
            string ordersFile = Path.Combine(_envPath, "Orders.xml");
            string xsltFile = Path.Combine(Directory.GetParent(_envPath)?.FullName ?? "", "XSLT", "HoaDonIn.xslt");

            if (!File.Exists(ordersFile) || !File.Exists(xsltFile)) return string.Empty;

            var doc = XDocument.Load(ordersFile);
            var orderElement = doc.Descendants("Order").FirstOrDefault(x => (string?)x.Attribute("id") == orderId);
            if (orderElement == null) return string.Empty;

            var transform = new XslCompiledTransform();
            transform.Load(xsltFile);

            using var stringWriter = new StringWriter();
            using var xmlReader = orderElement.CreateReader();
            transform.Transform(xmlReader, null, stringWriter);

            return stringWriter.ToString();
        }
    }
}
