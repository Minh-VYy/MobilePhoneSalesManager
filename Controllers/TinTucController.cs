using Microsoft.AspNetCore.Mvc;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers
{
    public class TinTucController : Controller
    {
        private readonly TinTucXmlService _tinTucService;

        public TinTucController(TinTucXmlService tinTucService)
        {
            _tinTucService = tinTucService;
        }

        public IActionResult Index(string? keyword, string? category = "ALL")
        {
            ViewBag.Keyword = keyword ?? "";
            ViewBag.CurrentCategory = category ?? "ALL";

            var items = _tinTucService.SearchAndFilter(keyword, category);
            ViewBag.HotNews = _tinTucService.GetHotNews(3);
            return View(items);
        }

        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var article = _tinTucService.GetById(id);
            if (article == null) return NotFound();

            _tinTucService.IncrementViews(id);

            var related = _tinTucService.GetAll()
                .Where(x => x.MaTT != id && (x.DanhMuc == article.DanhMuc || x.NoiBat))
                .Take(4)
                .ToList();

            ViewBag.RelatedNews = related;
            return View(article);
        }
    }
}
