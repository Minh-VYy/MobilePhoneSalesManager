namespace QuanLyBanDienThoai.Models;

public sealed record SupportMessage(long Id, string SenderRole, string SenderName, string Text, DateTimeOffset SentAt, string ClientId, SupportContext? Context = null, string Kind = "Text");
public sealed record SupportConversation(string Id, string CustomerName, string Preview, DateTimeOffset UpdatedAt, int Unread);
public sealed record ChatViewModel(bool IsAdmin = false, bool IsWidget = false, string? ProductId = null, string? ConversationId = null, string? OrderId = null);
public sealed record SupportContext(string Kind, string ReferenceId, string Title, string Image, string Description, decimal Amount,
    string Status, bool Available, DateTimeOffset UpdatedAt, string ChangedBy, string? Url = null);
public sealed record SupportChatState(long ContextVersion, SupportContext? Context);
