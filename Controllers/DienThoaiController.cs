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

        // 1. XML Web Service Endpoint (Trả về dữ liệu Web Service XML thô)
        [HttpGet]
        public IActionResult XmlApi()
        {
            string xmlPath = _dienThoaiService.GetXmlPath();
            string xmlContent = System.IO.File.ReadAllText(xmlPath);
            return Content(xmlContent, "application/xml", System.Text.Encoding.UTF8);
        }

        // 2. RSS 2.0 Feed Generator Endpoint (Mô tả ứng dụng RSS XML Feed)
        [HttpGet]
        public IActionResult RssFeed()
        {
            var items = _dienThoaiService.GetAll();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<rss version=\"2.0\">");
            sb.AppendLine("  <channel>");
            sb.AppendLine("    <title>PhoneStore 3D — RSS Feed Sản Phẩm Điện Thoại XML</title>");
            sb.AppendLine("    <link>https://phonestore3d.xml</link>");
            sb.AppendLine("    <description>Kênh tin RSS XML cập nhật danh mục điện thoại di động NoSQL XML mới nhất</description>");
            sb.AppendLine("    <language>vi-vn</language>");

            foreach (var item in items)
            {
                sb.AppendLine("    <item>");
                sb.AppendLine($"      <title><![CDATA[{item.TenDienThoai} ({item.TenHang})]]></title>");
                sb.AppendLine($"      <description><![CDATA[Giá khuyến mãi: {item.GiaGiamFormatted} (Giá niêm yết: {item.GiaGoiFormatted}) | RAM: {item.Ram}, ROM: {item.Rom}, Màu: {item.MauSac}. Số lượng trong kho: {item.SoLuongTon} chiếc.]]></description>");
                sb.AppendLine($"      <link>/DienThoai/Index?keyword={item.MaDienThoai}</link>");
                sb.AppendLine($"      <guid>{item.MaDienThoai}</guid>");
                sb.AppendLine("    </item>");
            }

            sb.AppendLine("  </channel>");
            sb.AppendLine("</rss>");

            return Content(sb.ToString(), "application/xml", System.Text.Encoding.UTF8);
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

