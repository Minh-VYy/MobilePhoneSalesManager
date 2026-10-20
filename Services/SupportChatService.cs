using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Xml.Linq;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public sealed class SupportChatService(PrivateXmlStore store)
{
    private readonly ConcurrentDictionary<Guid, ChatSubscription> _subscriptions = new();
    public List<SupportConversation> Conversations() => store.Read("SupportChats.xml", "Conversations", root => root.Elements("Conversation").Select(x =>
    {
        var last = x.Elements("Message").Last();
        var seen = (long?)x.Attribute("AdminRead") ?? 0;
        return new SupportConversation((string)x.Attribute("id")!, (string)x.Attribute("name")!, (string)last.Element("Text")!,
            DateTimeOffset.Parse((string)last.Attribute("at")!), x.Elements("Message").Count(m => (long)m.Attribute("id")! > seen && (string?)m.Attribute("role") == "User"));
    }).OrderByDescending(x => x.UpdatedAt).ToList());

    public bool Exists(string id) => store.Read("SupportChats.xml", "Conversations", root => Find(root, id) != null);
    public List<SupportMessage> Messages(string id, long after = 0, long? before = null) => store.Read("SupportChats.xml", "Conversations", root =>
        (Find(root, id)?.Elements("Message") ?? []).Where(x => (long)x.Attribute("id")! > after && (before == null || (long)x.Attribute("id")! < before))
        .TakeLast(before != null || after == 0 ? 50 : 200).Select(Map).ToList());
    public int Unread(string id) => store.Read("SupportChats.xml", "Conversations", root =>
    {
        var conversation = Find(root, id);
        var seen = (long?)conversation?.Attribute("CustomerRead") ?? 0;
        return conversation?.Elements("Message").Count(x => (long)x.Attribute("id")! > seen && (string?)x.Attribute("role") == "Admin") ?? 0;
    });

    public SupportChatState State(string id) => store.Read("SupportChats.xml", "Conversations", root =>
    {
        var conversation = Find(root, id);
        return new SupportChatState((long?)conversation?.Attribute("ContextVersion") ?? 0, MapContext(conversation?.Element("Context")));
    });

    public void SelectContext(string id, string name, bool admin, SupportContext? context, string clientId)
    {
        store.Update("SupportChats.xml", "Conversations", root =>
        {
            var conversation = GetOrCreate(root, id, name, admin);
            var role = admin ? "Admin" : "User";
            if (conversation.Elements("Message").Any(x => (string?)x.Attribute("clientId") == clientId && (string?)x.Attribute("role") == role)) return false;
            var current = MapContext(conversation.Element("Context"));
            if (current?.Kind == context?.Kind && current?.ReferenceId == context?.ReferenceId) return false;
            conversation.Element("Context")?.Remove();
            if (context != null) conversation.Add(ContextElement(context));
            var next = NextId(conversation);
            conversation.SetAttributeValue("ContextVersion", next);
            var text = context == null ? "Đã bỏ nội dung đang tư vấn." : context.Kind == "Product" ? "Đã chọn sản phẩm cần tư vấn." : "Đã chọn đơn hàng cần hỗ trợ.";
            conversation.Add(MessageElement(next, name, role, clientId, text, context, context == null ? "ContextCleared" : "Context"));
            return true;
        });
        Publish(id);
    }

    public SupportMessage Send(string id, string name, bool admin, string text, string clientId, long? contextVersion = null)
    {
        var message = store.Update("SupportChats.xml", "Conversations", root =>
        {
            var conversation = GetOrCreate(root, id, name, admin);
            var role = admin ? "Admin" : "User";
            var duplicate = conversation.Elements("Message").FirstOrDefault(x => (string?)x.Attribute("clientId") == clientId && (string?)x.Attribute("role") == role);
            if (duplicate != null) return Map(duplicate);
            if (contextVersion.HasValue && contextVersion != ((long?)conversation.Attribute("ContextVersion") ?? 0))
                throw new InvalidOperationException("Nội dung tư vấn vừa thay đổi. Hãy kiểm tra thẻ phía trên rồi gửi lại; tin nhắn của bạn vẫn được giữ.");
            var element = MessageElement(NextId(conversation), name, role, clientId, text.Trim(), MapContext(conversation.Element("Context")), "Text");
            conversation.Add(element);
            return Map(element);
        });
        Publish(id);
        return message;
    }

    public void MarkRead(string id, bool admin, long through)
    {
        store.Update("SupportChats.xml", "Conversations", root =>
        {
            var conversation = Find(root, id);
            if (conversation == null) return false;
            var key = admin ? "AdminRead" : "CustomerRead";
            var last = (long?)conversation.Elements("Message").LastOrDefault()?.Attribute("id") ?? 0;
            conversation.SetAttributeValue(key, Math.Max((long?)conversation.Attribute(key) ?? 0, Math.Min(through, last)));
            return true;
        });
    }

    public ChatSubscription Subscribe(string accountId, bool admin)
    {
        if (_subscriptions.Values.Count(x => x.AccountId == accountId) >= 8) throw new InvalidOperationException("Bạn đang mở quá nhiều cửa sổ chat.");
        var id = Guid.NewGuid();
        var subscription = new ChatSubscription(accountId, admin, () => _subscriptions.TryRemove(id, out _));
        _subscriptions[id] = subscription;
        return subscription;
    }
    private void Publish(string conversationId)
    {
        foreach (var subscription in _subscriptions.Values)
            if (subscription.IsAdmin || subscription.AccountId == conversationId) subscription.Channel.Writer.TryWrite(conversationId);
    }
    private static XElement? Find(XElement root, string id) => root.Elements("Conversation").FirstOrDefault(x => (string?)x.Attribute("id") == id);
    private static XElement GetOrCreate(XElement root, string id, string name, bool admin)
    {
        var conversation = Find(root, id);
        if (conversation != null) return conversation;
        if (admin) throw new InvalidOperationException("Cuộc hội thoại không còn tồn tại.");
        conversation = new XElement("Conversation", new XAttribute("id", id), new XAttribute("name", name));
        root.Add(conversation);
        return conversation;
    }
    private static long NextId(XElement conversation) => ((long?)conversation.Elements("Message").LastOrDefault()?.Attribute("id") ?? 0) + 1;
    private static XElement MessageElement(long id, string name, string role, string clientId, string text, SupportContext? context, string kind) =>
        new("Message", new XAttribute("id", id), new XAttribute("role", role), new XAttribute("name", name), new XAttribute("clientId", clientId),
            new XAttribute("at", DateTimeOffset.UtcNow.ToString("O")), new XAttribute("kind", kind), new XElement("Text", text), context == null ? null : ContextElement(context));
    private static XElement ContextElement(SupportContext context) => new("Context", new XAttribute("kind", context.Kind), new XAttribute("referenceId", context.ReferenceId),
        new XAttribute("at", context.UpdatedAt.ToString("O")), new XAttribute("by", context.ChangedBy), new XElement("Title", context.Title), new XElement("Image", context.Image),
        new XElement("Description", context.Description), new XElement("Amount", context.Amount), new XElement("Status", context.Status), new XElement("Available", context.Available));
    private static SupportContext? MapContext(XElement? x) => x == null ? null : new((string)x.Attribute("kind")!, (string)x.Attribute("referenceId")!,
        (string?)x.Element("Title") ?? "", (string?)x.Element("Image") ?? "", (string?)x.Element("Description") ?? "", (decimal?)x.Element("Amount") ?? 0,
        (string?)x.Element("Status") ?? "", (bool?)x.Element("Available") ?? false, DateTimeOffset.Parse((string)x.Attribute("at")!), (string?)x.Attribute("by") ?? "");
    private static SupportMessage Map(XElement x) => new((long)x.Attribute("id")!, (string)x.Attribute("role")!, (string)x.Attribute("name")!,
        (string)x.Element("Text")!, DateTimeOffset.Parse((string)x.Attribute("at")!), (string)x.Attribute("clientId")!, MapContext(x.Element("Context")), (string?)x.Attribute("kind") ?? "Text");
}

public sealed class ChatSubscription(string accountId, bool admin, Action onDispose) : IDisposable
{
    public string AccountId { get; } = accountId;
    public bool IsAdmin { get; } = admin;
    public Channel<string> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<string>(new BoundedChannelOptions(16) { FullMode = BoundedChannelFullMode.DropOldest });
    public void Dispose() { onDispose(); Channel.Writer.TryComplete(); }
}
