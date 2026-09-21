using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace QuanLyBanDienThoai.Services
{
    public class XsltTransformService
    {
        private readonly string _contentRootPath;

        public XsltTransformService(IWebHostEnvironment env)
        {
            _contentRootPath = env.ContentRootPath;
        }

        public string TransformXmlToHtml(string xmlRelativePath, string xsltRelativePath)
        {
            string xmlPath = Path.Combine(_contentRootPath, xmlRelativePath);
            string xsltPath = Path.Combine(_contentRootPath, xsltRelativePath);

            if (!File.Exists(xmlPath) || !File.Exists(xsltPath))
            {
                return $"<div class='alert alert-danger'>Không tìm thấy tệp XML ({xmlPath}) hoặc XSLT ({xsltPath})</div>";
            }

            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(xsltPath);

                using (StringWriter sw = new StringWriter())
                using (XmlWriter writer = XmlWriter.Create(sw, xslt.OutputSettings))
                {
                    xslt.Transform(xmlPath, writer);
                    return sw.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"<div class='alert alert-danger'>Lỗi XSLT Transform: {ex.Message}</div>";
            }
        }

        public string TransformSingleInvoiceToHtml(string maHd)
        {
            string xmlPath = Path.Combine(_contentRootPath, "Data", "HoaDon.xml");
            string xsltPath = Path.Combine(_contentRootPath, "XSLT", "HoaDonIn.xslt");

            if (!File.Exists(xmlPath) || !File.Exists(xsltPath))
            {
                return "<div class='alert alert-danger'>Không tìm thấy dữ liệu hóa đơn hoặc mẫu XSLT.</div>";
            }

            try
            {
                XDocument doc = XDocument.Load(xmlPath);
                XElement? elem = doc.XPathSelectElement($"//HoaDon[MaHD='{maHd}']");

                if (elem == null)
                {
                    return $"<div class='alert alert-warning'>Không tìm thấy hóa đơn mã {maHd}</div>";
                }

                XDocument tempDoc = new XDocument(new XElement("DanhSachHoaDon", elem));

                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(xsltPath);

                StringBuilder sb = new StringBuilder();
                using (XmlReader reader = tempDoc.CreateReader())
                using (StringWriter sw = new StringWriter(sb))
                using (XmlWriter writer = XmlWriter.Create(sw, xslt.OutputSettings))
                {
                    xslt.Transform(reader, writer);
                    return sw.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"<div class='alert alert-danger'>Lỗi Transform Hóa đơn: {ex.Message}</div>";
            }
        }
    }
}
