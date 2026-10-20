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
public class SupportController(SupportChatService chat, AccountXmlService accounts) : Controller
{
    private bool IsAdmin => User.IsInRole("Admin");
    private string AccountId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string? ConversationId(string? requested) => IsAdmin ? requested : AccountId;
    public IActionResult Index(string? productId = null, string? conversationId = null) => View(new ChatViewModel(IsAdmin, false, productId,
        IsAdmin && conversationId != null && chat.Exists(conversationId) ? conversationId : null));

    [HttpGet]
    public IActionResult Messages(string? conversationId, long after = 0, long? before = null)
    {
        var id = ConversationId(conversationId);
        if (string.IsNullOrEmpty(id) || after < 0 || before <= 0) return BadRequest();
        return Json(new { conversationId = id, messages = chat.Messages(id, after, before) });
    }
    [HttpGet]
    public IActionResult Inbox() => Json(IsAdmin ? new { conversations = chat.Conversations(), unread = 0 } : new { conversations = new List<SupportConversation>(), unread = chat.Unread(AccountId) });

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("chat")]
    public IActionResult Send(string? conversationId, string text, string clientId)
    {
        var id = ConversationId(conversationId);
        if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(text) || text.Length > 2000 || !Guid.TryParse(clientId, out _) || (IsAdmin && !chat.Exists(id)))
            return BadRequest(new { error = "Chọn cuộc trò chuyện và nhập tin nhắn từ 1 đến 2.000 ký tự." });
        var message = chat.Send(id, User.FindFirstValue("DisplayName") ?? User.Identity!.Name!, IsAdmin, text, clientId);
        return Json(new { conversationId = id, message });
    }
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
