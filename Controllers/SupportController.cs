using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuanLyBanDienThoai.Models;
using QuanLyBanDienThoai.Services;

namespace QuanLyBanDienThoai.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SupportController(SupportChatService chat, AccountXmlService accounts, DienThoaiXmlService phones, PhoneDetailXmlService details, CustomerOrderService orders) : Controller
{
    private bool IsAdmin => User.IsInRole("Admin");
    private string AccountId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string? ConversationId(string? requested) => IsAdmin ? requested : AccountId;
    public IActionResult Index(string? productId = null, string? conversationId = null, string? orderId = null)
    {
        var selectedConversation = IsAdmin && conversationId != null && chat.Exists(conversationId) ? conversationId : null;
        var owner = ConversationId(selectedConversation);
        var selectedOrder = !string.IsNullOrEmpty(owner) && orderId != null && ResolveContext("Order", orderId, owner) != null ? orderId : null;
        return View(new ChatViewModel(IsAdmin, false, productId, selectedConversation, selectedOrder));
    }

    [HttpGet]
    public IActionResult Messages(string? conversationId, long after = 0, long? before = null)
    {
        var id = ConversationId(conversationId);
        if (string.IsNullOrEmpty(id) || after < 0 || before <= 0) return BadRequest();
        if (IsAdmin && !chat.Exists(id)) return NotFound();
        lock (PrivateXmlStore.Gate)
        {
            var state = chat.State(id);
            var ownerOrders = orders.GetAll(id);
            var messages = chat.Messages(id, after, before).Select(x => x with { Context = VisibleMessageContext(x.Context, ownerOrders) }).ToList();
            return Json(new { conversationId = id, messages, state.ContextVersion, context = CurrentContext(state, id), hasActiveOrders = ownerOrders.Any(x => x.IsInProgress) });
        }
    }
    [HttpGet]
    public IActionResult Inbox() => Json(IsAdmin ? new { conversations = chat.Conversations(), unread = 0 } : new { conversations = new List<SupportConversation>(), unread = chat.Unread(AccountId) });

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("chat")]
    public IActionResult Send(string? conversationId, string text, string clientId, long? contextVersion = null)
    {
        var id = ConversationId(conversationId);
        if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(text) || text.Length > 2000 || !Guid.TryParse(clientId, out _) || (IsAdmin && !chat.Exists(id)))
            return BadRequest(new { error = "Chọn cuộc trò chuyện và nhập tin nhắn từ 1 đến 2.000 ký tự." });
        try
        {
            lock (PrivateXmlStore.Gate)
            {
                var message = chat.Send(id, User.FindFirstValue("DisplayName") ?? User.Identity!.Name!, IsAdmin, text, clientId, CurrentContext(chat.State(id), id), contextVersion);
                return Json(new { conversationId = id, message = message with { Context = VisibleMessageContext(message.Context, orders.GetAll(id)) } });
            }
        }
        catch (InvalidOperationException error) { return Conflict(new { error = error.Message }); }
    }

    [HttpGet]
    public IActionResult ContextOptions(string? conversationId, string kind = "Product", string? keyword = null)
    {
        var id = ConversationId(conversationId);
        if (string.IsNullOrEmpty(id) || (IsAdmin && !chat.Exists(id)) || kind is not ("Product" or "Order")) return BadRequest();
        keyword = (keyword ?? "").Trim();
        if (keyword.Length > 100) return BadRequest();
        lock (PrivateXmlStore.Gate)
        {
            var items = kind == "Product"
                ? phones.GetAll().Where(x => string.IsNullOrEmpty(keyword) || (x.TenSP + " " + x.MaSP + " " + x.TenHang).Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.SoLuongTon > 0).ThenBy(x => x.TenSP).Take(20).Select(ProductContext).ToList()
                : orders.GetAll(id).Where(x => x.IsInProgress && (string.IsNullOrEmpty(keyword) || (x.Id + " " + string.Join(" ", x.Items.Select(i => i.Name))).Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Status == "Shipping" ? 0 : x.Status == "Confirmed" ? 1 : x.Status == "Pending" ? 2 : 3)
                    .ThenByDescending(x => x.CreatedAt).Take(20).Select(OrderContext).ToList();
            return Json(new { conversationId = id, items });
        }
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("chat")]
    public IActionResult SelectContext(string? conversationId, string kind, string? referenceId, string clientId)
    {
        var id = ConversationId(conversationId);
        if (string.IsNullOrEmpty(id) || (IsAdmin && !chat.Exists(id)) || kind is not ("Product" or "Order" or "Clear") || !Guid.TryParse(clientId, out _) || referenceId?.Length > 100) return BadRequest();
        lock (PrivateXmlStore.Gate)
        {
            var context = kind == "Clear" ? null : ResolveContext(kind, referenceId ?? "", id);
            if (kind != "Clear" && context == null) return NotFound(new { error = "Không tìm thấy sản phẩm hoặc đơn hàng đang xử lý của khách trong cuộc trò chuyện này. Không thể chọn đơn đã giao hoặc đã hủy." });
            var name = User.FindFirstValue("DisplayName") ?? User.Identity!.Name!;
            chat.SelectContext(id, name, IsAdmin, context == null ? null : context with { ChangedBy = name }, clientId);
            var state = chat.State(id);
            return Json(new { conversationId = id, state.ContextVersion, context = CurrentContext(state, id), hasActiveOrders = HasActiveOrders(id) });
        }
    }

    private SupportContext? ResolveContext(string kind, string referenceId, string owner)
    {
        if (kind == "Product") return phones.GetById(referenceId) is { } product ? ProductContext(product) : null;
        var order = kind == "Order" ? orders.Get(referenceId) : null;
        return order != null && order.AccountId == owner && order.IsInProgress ? OrderContext(order) : null;
    }
    private bool HasActiveOrders(string owner) => orders.GetAll(owner).Any(x => x.IsInProgress);
    // Past messages keep their snapshots, but an order must still belong to this conversation's customer.
    private SupportContext? VisibleMessageContext(SupportContext? context, IReadOnlyCollection<BuyerOrder> ownerOrders) => context == null ? null
        : context.Kind == "Order" && !ownerOrders.Any(x => x.Id == context.ReferenceId) ? null : Link(context);
    private SupportContext? CurrentContext(SupportChatState state, string owner) => state.Context == null ? null
        : ResolveContext(state.Context.Kind, state.Context.ReferenceId, owner) is { } current ? current with { UpdatedAt = state.Context.UpdatedAt, ChangedBy = state.Context.ChangedBy }
        : state.Context.Kind == "Order" ? null
        : state.Context with { Available = false, Status = "Không còn khả dụng", Url = null };
    private SupportContext ProductContext(DienThoaiModel phone)
    {
        var meta = details.GetProductMetadata(phone.MaSP);
        var description = string.Join(" · ", new[] { phone.ThongSo.BoNhoTrong, meta?.ColorName, meta?.ConditionDescription, meta?.WarrantyMonths > 0 ? $"Bảo hành {meta.WarrantyMonths} tháng" : null }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (description.Length > 400) description = description[..400] + "…";
        return Link(new SupportContext("Product", phone.MaSP, phone.TenSP, string.IsNullOrWhiteSpace(meta?.Image) ? phone.HinhAnh : meta.Image,
            description, phone.GiaBan, phone.SoLuongTon > 0 ? $"Còn {phone.SoLuongTon} máy" : "Hết hàng", true, DateTimeOffset.UtcNow, ""));
    }
    private SupportContext OrderContext(BuyerOrder order) => Link(new SupportContext("Order", order.Id, "Đơn hàng " + order.Id,
        order.Items.FirstOrDefault()?.Image ?? "", string.Join(", ", order.Items.Take(2).Select(x => x.Name)) + (order.Items.Count > 2 ? $" và {order.Items.Count - 2} phiên bản khác" : "")
            + $" · {order.Items.Sum(x => x.Quantity)} máy", order.Total, order.StatusLabel, true, DateTimeOffset.UtcNow, ""));
    private SupportContext Link(SupportContext context) => context with { Url = Url.Action("Details", context.Kind == "Product" ? "DienThoai" : "Orders", new { id = context.ReferenceId }) };
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Read(string? conversationId, long through)
    {
        var id = ConversationId(conversationId);
        if (string.IsNullOrEmpty(id) || through < 0) return BadRequest();
        chat.MarkRead(id, IsAdmin, through);
        return NoContent();
    }

    [HttpGet]
    public async Task Events(CancellationToken cancellationToken)
    {
        ChatSubscription subscription;
        try { subscription = chat.Subscribe(AccountId, IsAdmin); }
        catch (InvalidOperationException) { Response.StatusCode = 429; return; }
        using (subscription)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Accel-Buffering"] = "no";
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            var auth = await HttpContext.AuthenticateAsync();
            try
            {
                await Response.WriteAsync("event: connected\ndata: {}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                while (!cancellationToken.IsCancellationRequested && auth.Properties?.ExpiresUtc > DateTimeOffset.UtcNow)
                {
                    using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    heartbeat.CancelAfter(TimeSpan.FromSeconds(20));
                    string? conversation = null;
                    try { conversation = await subscription.Channel.Reader.ReadAsync(heartbeat.Token); }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
                    if (accounts.Find(AccountId)?.SecurityStamp != User.FindFirstValue("SecurityStamp")) break;
                    await Response.WriteAsync(conversation == null ? ": heartbeat\n\n" : $"event: change\ndata: {JsonSerializer.Serialize(new { conversationId = conversation })}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }
    }
}
