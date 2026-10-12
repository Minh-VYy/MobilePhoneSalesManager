using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers
{
    public class DienThoaiController : Controller
    {
        private readonly DienThoaiXmlService _dienThoaiService;
        private readonly PhoneDetailXmlService _phoneDetailService;
        private readonly XmlValidationService _validationService;
        private readonly XsltTransformService _xsltService;

        public DienThoaiController(
            DienThoaiXmlService dienThoaiService,
            PhoneDetailXmlService phoneDetailService,
            XmlValidationService validationService,
            XsltTransformService xsltService)
        {
            _dienThoaiService = dienThoaiService;
            _phoneDetailService = phoneDetailService;
            _validationService = validationService;
            _xsltService = xsltService;
        }

        // Trang Catalogue sản phẩm cho người dùng
        public IActionResult Index(string keyword, string maHang, decimal? minGia, decimal? maxGia, string category = "ALL")
        {
            category = (category ?? "ALL").Trim().ToUpperInvariant();
            if (category != "IPHONE" && category != "ANDROID") category = "ALL";

            var isAdmin = HttpContext.Session.GetString("Role") == "Admin";
            var allPhones = _dienThoaiService.GetAll();
            var seriesById = _phoneDetailService.GetModelSeriesMap();
            var totalVariantsBySeries = allPhones
                .GroupBy(phone => GetSeries(phone, seriesById), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var items = _dienThoaiService.SearchAndFilter(keyword, maHang, minGia, maxGia);
            if (category == "IPHONE")
                items = items.Where(p => string.Equals(p.MaHang, "APPLE", StringComparison.OrdinalIgnoreCase)).ToList();
            else if (category == "ANDROID")
                items = items.Where(p => !string.Equals(p.MaHang, "APPLE", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => p.SoLuongTon > 0)
                    .ThenByDescending(p => p.GiaBan)
                    .ToList();

            if (!isAdmin && category == "ALL" && string.IsNullOrWhiteSpace(keyword))
                items.Clear();

            items = items
                .GroupBy(phone => GetSeries(phone, seriesById), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var representative = group
                        .OrderByDescending(phone => phone.SoLuongTon > 0)
                        .ThenBy(phone => phone.GiaBan)
                        .First();
                    representative.ModelSeries = GetSeries(representative, seriesById);
                    representative.VariantCount = totalVariantsBySeries.GetValueOrDefault(representative.ModelSeries, group.Count());
                    representative.VariantSummary = BuildVariantSummary(allPhones
                        .Where(phone => string.Equals(GetSeries(phone, seriesById), representative.ModelSeries, StringComparison.OrdinalIgnoreCase)));
                    representative.GiaBan = group.Min(phone => phone.GiaBan);
                    representative.SoLuongTon = group.Sum(phone => phone.SoLuongTon);
                    return representative;
                })
                .OrderByDescending(phone => phone.SoLuongTon > 0)
                .ThenBy(phone => phone.GiaBan)
                .ToList();

            var model = new SearchFilterViewModel
            {
                Keyword = keyword ?? "",
                MaHang = maHang ?? "ALL",
                Category = category,
                MinGia = minGia,
                MaxGia = maxGia,
                Items = items
            };
            ViewBag.AllItems = allPhones;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.RecommendedIds = category == "ANDROID"
                ? items.Where(p => p.SoLuongTon > 0).Take(4).Select(p => p.MaSP).ToHashSet()
                : new HashSet<string>();
            return View(model);
        }
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var phone = _dienThoaiService.GetById(id);
            if (phone == null) return NotFound();

            var allPhones = _dienThoaiService.GetAll();
            var related = allPhones.Where(p => p.MaSP != phone.MaSP && p.MaHang == phone.MaHang)
                .OrderByDescending(p => p.SoLuongTon > 0)
                .ThenByDescending(p => p.GiaBan)
                .Take(4)
                .ToList();
            if (related.Count < 4)
                related.AddRange(allPhones.Where(p => p.MaSP != phone.MaSP && p.MaHang != phone.MaHang)
                    .OrderByDescending(p => p.SoLuongTon > 0).ThenByDescending(p => p.GiaBan)
                    .Take(4 - related.Count));

            var variants = _phoneDetailService.GetVariants(phone.MaSP);
            if (variants.Count == 0)
                variants.Add(CreateFallbackVariant(phone));

            ViewBag.IsAdmin = HttpContext.Session.GetString("Role") == "Admin";
            var model = new DienThoaiChiTietViewModel
            {
                SanPham = phone,
                SanPhamLienQuan = related,
                DanhMuc = string.Equals(phone.MaHang, "APPLE", StringComparison.OrdinalIgnoreCase) ? "IPHONE" : "ANDROID",
                Metadata = variants.FirstOrDefault(x => x.ProductId == phone.MaSP) ?? new PhoneDetailMetadata(),
                BienThe = variants,
                VariantOptions = variants
                    .Select(metadata =>
                    {
                        var variantPhone = allPhones.FirstOrDefault(item => string.Equals(item.MaSP, metadata.ProductId, StringComparison.OrdinalIgnoreCase));
                        return variantPhone == null ? null : BuildVariantOption(variantPhone, metadata);
                    })
                    .OfType<PhoneVariantOptionViewModel>()
                    .ToList(),
                DanhGia = _phoneDetailService.GetReviews(phone.MaSP)
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Variant(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            var phone = _dienThoaiService.GetById(id);
            if (phone == null) return NotFound();

            var metadata = _phoneDetailService.GetProductMetadata(id) ?? CreateFallbackVariant(phone);
            var conditionLabel = metadata.ConditionType == "New" ? "M\u00e1y m\u1edbi" : "M\u00e1y \u0111\u00e3 qua s\u1eed d\u1ee5ng";
            var conditionText = string.IsNullOrWhiteSpace(metadata.ConditionGrade)
                ? conditionLabel
                : $"{conditionLabel} \u00b7 {metadata.ConditionGrade}";
            var warrantyText = metadata.WarrantyMonths > 0
                ? $"{metadata.WarrantyMonths} th\u00e1ng"
                : "Li\u00ean h\u1ec7 \u0111\u1ec3 x\u00e1c nh\u1eadn";

            var smsMessage = $"Ch\u00e0o PhoneStore, t\u00f4i mu\u1ed1n \u0111\u01b0\u1ee3c t\u01b0 v\u1ea5n {phone.TenSP}, t\u00ecnh tr\u1ea1ng {conditionText}, m\u00e0u {metadata.ColorName}, dung l\u01b0\u1ee3ng {phone.ThongSo.BoNhoTrong}, b\u1ea3o h\u00e0nh {warrantyText}.";
            var smsUrl = $"sms:0901176222?body={Uri.EscapeDataString(smsMessage)}";

            return Json(new
            {
                productId = phone.MaSP,
                productName = phone.TenSP,
                modelSeries = metadata.ModelSeries,
                brand = phone.TenHang,
                description = phone.MoTa,
                conditionDescription = metadata.ConditionDescription,
                price = phone.GiaBan,
                originalPrice = metadata.OriginalPrice,
                stock = phone.SoLuongTon,
                image = string.IsNullOrWhiteSpace(metadata.Image) ? phone.HinhAnh : metadata.Image,
                color = metadata.ColorName,
                colorCode = metadata.ColorCode,
                conditionType = metadata.ConditionType,
                conditionGrade = metadata.ConditionGrade,
                conditionText,
                conditionValue = $"{metadata.ConditionType}|{metadata.ConditionGrade}",
                warrantyMonths = metadata.WarrantyMonths,
                warrantyText,
                storage = metadata.Storage,
                storageLabel = string.IsNullOrWhiteSpace(phone.ThongSo.BoNhoTrong)
                    ? $"{metadata.Storage} GB"
                    : phone.ThongSo.BoNhoTrong,
                screen = phone.ThongSo.ManHinh,
                resolution = metadata.Resolution,
                ram = phone.ThongSo.RAM,
                chipset = phone.ThongSo.Chip,
                camera = metadata.Camera,
                battery = phone.ThongSo.Pin,
                chargingTechnology = metadata.ChargingTechnology,
                ports = metadata.Ports,
                weight = metadata.Weight,
                sku = metadata.Sku,
                detailsUrl = Url.Action(nameof(Details), new { id = phone.MaSP }),
                smsUrl,
            });
        }

        [HttpGet]
        public IActionResult Cart()
        {
            ViewData["Title"] = "Giỏ hàng";
            ViewBag.IsAdmin = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(string id, string customerName, int rating, string content)
        {
            if (string.IsNullOrWhiteSpace(id) || _dienThoaiService.GetById(id) == null) return NotFound();
            if (string.IsNullOrWhiteSpace(customerName) || customerName.Length > 80 || rating is < 1 or > 5 || string.IsNullOrWhiteSpace(content) || content.Length > 1200)
            {
                TempData["ReviewError"] = "Vui lòng nhập tên, chọn số sao và viết nhận xét tối đa 1.200 ký tự.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _phoneDetailService.AddReview(new ProductReviewModel
            {
                ProductId = id,
                CustomerName = customerName.Trim(),
                Rating = rating,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            });
            TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá.";
            return Redirect($"{Url.Action(nameof(Details), new { id })}#reviews");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitOrder(CustomerOrderRequestModel request)
        {
            var products = _dienThoaiService.GetAll().ToDictionary(x => x.MaSP, StringComparer.OrdinalIgnoreCase);
            var groupedItems = (request.Items ?? new List<CustomerOrderRequestItem>())
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductId) && x.Quantity > 0)
                .GroupBy(x => x.ProductId, StringComparer.OrdinalIgnoreCase)
                .Select(x => new { ProductId = x.Key, Quantity = x.Sum(y => y.Quantity) })
                .ToList();

            var validContact = !string.IsNullOrWhiteSpace(request.CustomerName) && request.CustomerName.Length <= 100
                && System.Text.RegularExpressions.Regex.IsMatch(request.Phone ?? string.Empty, @"^0[35789]\d{8}$")
                && !string.IsNullOrWhiteSpace(request.Address) && request.Address.Length <= 250;
            if (!validContact || groupedItems.Count == 0 || groupedItems.Any(x => !products.TryGetValue(x.ProductId, out var p) || x.Quantity > p.SoLuongTon))
            {
                TempData["OrderError"] = "Thông tin đặt hàng chưa hợp lệ hoặc số lượng vượt tồn kho. Vui lòng kiểm tra lại giỏ hàng.";
                return RedirectToAction(nameof(Cart));
            }

            var resolvedItems = groupedItems.Select(x => (products[x.ProductId], x.Quantity)).ToList();
            var requestId = _phoneDetailService.AddOrderRequest(request, resolvedItems);
            TempData["OrderSuccess"] = $"Đã nhận yêu cầu {requestId}. Nhân viên sẽ liên hệ xác nhận đơn hàng.";
            return RedirectToAction(nameof(Cart), new { completed = true });
        }

        private static string GetSeries(DienThoaiModel phone, IReadOnlyDictionary<string, string> seriesById)
        {
            if (seriesById.TryGetValue(phone.MaSP, out var series) && !string.IsNullOrWhiteSpace(series))
                return series.Trim();

            var name = phone.TenSP.Split(" - ", 2)[0].Trim();
            return System.Text.RegularExpressions.Regex.Replace(name, @"\s+\d+\s*GB$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static string BuildVariantSummary(IEnumerable<DienThoaiModel> variants)
        {
            var list = variants.ToList();
            var conditions = list
                .Select(phone => System.Text.RegularExpressions.Regex.Match(phone.TenSP, @"(?:M?i|Likenew|C?)\s*\d{2,3}%?", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2);
            var storage = list
                .Select(phone => phone.ThongSo.BoNhoTrong)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => int.TryParse(System.Text.RegularExpressions.Regex.Match(value, @"\d+").Value, out var size) ? size : int.MaxValue)
                .Take(4);
            var parts = conditions.Concat(storage).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return parts.Count == 0 ? $"{list.Count} phiên bản" : string.Join(" · ", parts);
        }

        private PhoneVariantOptionViewModel BuildVariantOption(DienThoaiModel phone, PhoneDetailMetadata metadata)
        {
            var conditionLabel = metadata.ConditionType == "New" ? "Máy mới" : "Máy đã qua sử dụng";
            var conditionText = string.IsNullOrWhiteSpace(metadata.ConditionGrade)
                ? conditionLabel
                : $"{conditionLabel} · {metadata.ConditionGrade}";
            var warrantyText = metadata.WarrantyMonths > 0
                ? $"{metadata.WarrantyMonths} tháng"
                : "Liên hệ để xác nhận";
            var storage = string.IsNullOrWhiteSpace(metadata.Storage)
                ? System.Text.RegularExpressions.Regex.Match(phone.ThongSo.BoNhoTrong ?? string.Empty, @"\d+").Value
                : metadata.Storage;
            var storageLabel = string.IsNullOrWhiteSpace(phone.ThongSo.BoNhoTrong)
                ? $"{storage} GB"
                : phone.ThongSo.BoNhoTrong;
            var image = string.IsNullOrWhiteSpace(metadata.Image) ? phone.HinhAnh : metadata.Image;
            var smsMessage = $"Chào PhoneStore, tôi muốn được tư vấn {phone.TenSP}, tình trạng {conditionText}, màu {metadata.ColorName}, dung lượng {storageLabel}, bảo hành {warrantyText}.";

            return new PhoneVariantOptionViewModel
            {
                ProductId = phone.MaSP,
                ProductName = phone.TenSP,
                ModelSeries = string.IsNullOrWhiteSpace(metadata.ModelSeries) ? phone.TenSP : metadata.ModelSeries,
                Brand = phone.TenHang,
                Description = phone.MoTa,
                ConditionDescription = metadata.ConditionDescription,
                Price = phone.GiaBan,
                OriginalPrice = metadata.OriginalPrice,
                Stock = phone.SoLuongTon,
                Image = image,
                Color = metadata.ColorName,
                ColorCode = metadata.ColorCode,
                ConditionType = metadata.ConditionType,
                ConditionGrade = metadata.ConditionGrade,
                ConditionText = conditionText,
                ConditionValue = $"{metadata.ConditionType}|{metadata.ConditionGrade}",
                WarrantyMonths = metadata.WarrantyMonths,
                WarrantyText = warrantyText,
                Storage = storage,
                StorageLabel = storageLabel,
                Screen = phone.ThongSo.ManHinh,
                Resolution = metadata.Resolution,
                Ram = phone.ThongSo.RAM,
                Chipset = phone.ThongSo.Chip,
                Camera = metadata.Camera,
                Battery = phone.ThongSo.Pin,
                ChargingTechnology = metadata.ChargingTechnology,
                Ports = metadata.Ports,
                Weight = metadata.Weight,
                Sku = metadata.Sku,
                DetailsUrl = Url.Action(nameof(Details), new { id = phone.MaSP }) ?? string.Empty,
                SmsUrl = $"sms:0901176222?body={Uri.EscapeDataString(smsMessage)}"
            };
        }

        private static PhoneDetailMetadata CreateFallbackVariant(DienThoaiModel phone)
        {
            var nameParts = phone.TenSP.Split(" - ", 2);
            var color = nameParts.Length > 1 ? nameParts[1].Split('(')[0].Trim() : string.Empty;
            var grade = System.Text.RegularExpressions.Regex.Match(phone.TenSP, @"\d{2,3}%").Value;
            var storage = System.Text.RegularExpressions.Regex.Match(phone.ThongSo.BoNhoTrong ?? string.Empty, @"\d+").Value;
            return new PhoneDetailMetadata
            {
                ProductId = phone.MaSP,
                ModelSeries = nameParts[0],
                ConditionType = phone.TenSP.Contains("Mới", StringComparison.OrdinalIgnoreCase) ? "New" : "Used",
                ConditionGrade = grade,
                ConditionDescription = phone.MoTa,
                ColorName = color,
                Image = phone.HinhAnh,
                Sku = phone.MaSP,
                ScreenSize = phone.ThongSo.ManHinh,
                RAM = phone.ThongSo.RAM,
                Storage = storage
            };
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
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
            var items = _dienThoaiService.GetAll();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
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
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
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
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var item = _dienThoaiService.GetById(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(DienThoaiModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
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
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
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
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Login", "Account");
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
                sb.AppendLine($"      <title><![CDATA[{item.TenSP} ({item.TenHang})]]></title>");
                sb.AppendLine($"      <description><![CDATA[Giá bán: {item.GiaBan:N0} VNĐ | RAM: {item.ThongSo?.RAM}, ROM: {item.ThongSo?.BoNhoTrong}, Chip: {item.ThongSo?.Chip}. Số lượng tồn kho: {item.SoLuongTon} chiếc.]]></description>");
                sb.AppendLine($"      <link>/DienThoai/Index?keyword={item.MaSP}</link>");
                sb.AppendLine($"      <guid>{item.MaSP}</guid>");
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



