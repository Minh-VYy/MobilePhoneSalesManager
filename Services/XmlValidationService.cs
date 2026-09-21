using System.Xml;
using System.Xml.Schema;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services
{
    public class XmlValidationService
    {
        public ValidationResultModel ValidateXml(string xmlFilePath, string xsdFilePath)
        {
            var result = new ValidationResultModel
            {
                FileName = Path.GetFileName(xmlFilePath),
                SchemaName = Path.GetFileName(xsdFilePath),
                IsValid = true
            };

            if (!File.Exists(xmlFilePath))
            {
                result.IsValid = false;
                result.Errors.Add(new XmlValidationError { Message = $"Tệp XML '{xmlFilePath}' không tồn tại." });
                return result;
            }

            if (!File.Exists(xsdFilePath))
            {
                result.IsValid = false;
                result.Errors.Add(new XmlValidationError { Message = $"Tệp Schema XSD '{xsdFilePath}' không tồn tại." });
                return result;
            }

            try
            {
                XmlSchemaSet schemas = new XmlSchemaSet();
                schemas.Add("", xsdFilePath);

                XmlReaderSettings settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = schemas
                };

                settings.ValidationEventHandler += (sender, args) =>
                {
                    result.IsValid = false;
                    result.Errors.Add(new XmlValidationError
                    {
                        LineNumber = args.Exception.LineNumber,
                        LinePosition = args.Exception.LinePosition,
                        Severity = args.Severity.ToString(),
                        Message = args.Message
                    });
                };

                using (XmlReader reader = XmlReader.Create(xmlFilePath, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new XmlValidationError
                {
                    Message = $"Ngoại lệ khi đọc XML: {ex.Message}"
                });
            }

            return result;
        }

        public ValidationResultModel ValidateXmlContent(string xmlContent, string xsdFilePath)
        {
            var result = new ValidationResultModel
            {
                FileName = "Nội dung XML tạm",
                SchemaName = Path.GetFileName(xsdFilePath),
                IsValid = true
            };

            try
            {
                XmlSchemaSet schemas = new XmlSchemaSet();
                schemas.Add("", xsdFilePath);

                XmlReaderSettings settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = schemas
                };

                settings.ValidationEventHandler += (sender, args) =>
                {
                    result.IsValid = false;
                    result.Errors.Add(new XmlValidationError
                    {
                        LineNumber = args.Exception.LineNumber,
                        LinePosition = args.Exception.LinePosition,
                        Severity = args.Severity.ToString(),
                        Message = args.Message
                    });
                };

                using (StringReader sr = new StringReader(xmlContent))
                using (XmlReader reader = XmlReader.Create(sr, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new XmlValidationError
                {
                    Message = $"Lỗi định dạng XML: {ex.Message}"
                });
            }

            return result;
        }
    }
}
