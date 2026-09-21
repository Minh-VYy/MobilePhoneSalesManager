using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers
{
    public class HoaDonController : Controller
    {
        private readonly HoaDonXmlService _hoaDonService;
        private readonly DienThoaiXmlService _dienThoaiService;
        private readonly XmlValidationService _validationService;
        private readonly XsltTransformService _xsltService;

        public HoaDonController(
            HoaDonXmlService hoaDonService,
            DienThoaiXmlService dienThoaiService,
            XmlValidationService validationService,
            XsltTransformService xsltService)
        {
            _hoaDonService = hoaDonService;
            _dienThoaiService = dienThoaiService;
            _validationService = validationService;
            _xsltService = xsltService;
        }

        // Danh sách tất cả hóa đơn XML
        public IActionResult Index()
        {
            var invoices = _hoaDonService.GetAll();
            return View(invoices);
        }

        // Tạo hóa đơn mới (Bán hàng POS)
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.PhoneList = _dienThoaiService.GetAll();
            return View(new CreateHoaDonInputModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateHoaDonInputModel input)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PhoneList = _dienThoaiService.GetAll();
                return View(input);
            }

            var (success, message, newMaHD, valResult) = _hoaDonService.CreateInvoice(input);
            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Print), new { id = newMaHD });
            }

            ViewBag.ErrorMessage = message;
            ViewBag.ValidationResult = valResult;
            ViewBag.PhoneList = _dienThoaiService.GetAll();
            return View(input);
        }

        // In / Xem mẫu Hóa đơn bán lẻ được Transform trực tiếp qua XSLT
        public IActionResult Print(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var invoice = _hoaDonService.GetById(id);
            if (invoice == null) return NotFound();

            string htmlContent = _xsltService.TransformSingleInvoiceToHtml(id);
            ViewBag.HtmlContent = htmlContent;
            ViewBag.InvoiceCode = id;
            return View();
        }

        // Validate file HoaDon.xml với HoaDon.xsd
        public IActionResult ValidateXsd()
        {
            string xmlPath = _hoaDonService.GetXmlPath();
            string xsdPath = _hoaDonService.GetXsdPath();

            var result = _validationService.ValidateXml(xmlPath, xsdPath);
            return View(result);
        }
    }
}
