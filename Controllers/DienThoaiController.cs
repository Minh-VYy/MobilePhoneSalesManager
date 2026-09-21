using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers
{
    public class DienThoaiController : Controller
    {
        private readonly DienThoaiXmlService _dienThoaiService;
        private readonly XmlValidationService _validationService;
        private readonly XsltTransformService _xsltService;

        public DienThoaiController(
            DienThoaiXmlService dienThoaiService,
            XmlValidationService validationService,
            XsltTransformService xsltService)
        {
            _dienThoaiService = dienThoaiService;
            _validationService = validationService;
            _xsltService = xsltService;
        }

        // Trang Catalogue sản phẩm cho người dùng
        public IActionResult Index(string keyword, string maHang, decimal? minGia, decimal? maxGia)
        {
            var items = _dienThoaiService.SearchAndFilter(keyword, maHang, minGia, maxGia);
            var model = new SearchFilterViewModel
            {
                Keyword = keyword ?? "",
                MaHang = maHang ?? "ALL",
                MinGia = minGia,
                MaxGia = maxGia,
                Items = items
            };
            return View(model);
        }

        // View hiển thị chuyển đổi XSLT thuần
        public IActionResult XsltView()
        {
            string htmlContent = _xsltService.TransformXmlToHtml("Data/DienThoai.xml", "XSLT/DienThoaiCatalogue.xslt");
            ViewBag.HtmlContent = htmlContent;
            return View();
        }

        // Trang Quản lý CRUD dành cho Admin/Quản lý
        public IActionResult Manage()
        {
            var items = _dienThoaiService.GetAll();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new DienThoaiModel
            {
                MaHang = "APPLE",
                TenHang = "Apple",
                GiaBan = 10000000,
                SoLuongTon = 10
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DienThoaiModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tự gán tên hãng chuẩn theo mã hãng
            model.TenHang = GetTenHangByMa(model.MaHang);

            var (success, message, valResult) = _dienThoaiService.Add(model);
            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Manage));
            }

            ViewBag.ErrorMessage = message;
            ViewBag.ValidationResult = valResult;
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var item = _dienThoaiService.GetById(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(DienThoaiModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.TenHang = GetTenHangByMa(model.MaHang);

            var (success, message, valResult) = _dienThoaiService.Update(model);
            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Manage));
            }

            ViewBag.ErrorMessage = message;
            ViewBag.ValidationResult = valResult;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            bool deleted = _dienThoaiService.Delete(id);
            if (deleted)
            {
                TempData["SuccessMessage"] = $"Đã xóa sản phẩm {id} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể xóa sản phẩm {id}.";
            }
            return RedirectToAction(nameof(Manage));
        }

        // Kiểm tra tính hợp lệ XML với XSD Schema
        public IActionResult ValidateXsd()
        {
            string xmlPath = _dienThoaiService.GetXmlPath();
            string xsdPath = _dienThoaiService.GetXsdPath();

            var result = _validationService.ValidateXml(xmlPath, xsdPath);
            return View(result);
        }

        private string GetTenHangByMa(string maHang)
        {
            return maHang switch
            {
                "APPLE" => "Apple",
                "SAMSUNG" => "Samsung",
                "XIAOMI" => "Xiaomi",
                "OPPO" => "OPPO",
                "GOOGLE" => "Google",
                _ => "Khác"
            };
        }
    }
}
