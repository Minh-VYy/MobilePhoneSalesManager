namespace QuanLyBanDienThoai.Models;

public sealed class BuyerOrder
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Note { get; set; } = "";
    public List<BuyerOrderItem> Items { get; set; } = [];
    public List<OrderEvent> History { get; set; } = [];
    public decimal Total => Items.Sum(x => x.UnitPrice * x.Quantity);
    public string StatusLabel => StatusName(Status);
    public static string StatusName(string status) => status switch
    {
        "Pending" => "Chờ xác nhận",
        "Confirmed" => "Đã xác nhận",
        "Shipping" => "Đang giao hàng",
        "Delivered" => "Đã giao hàng",
        "Cancelled" => "Đã hủy",
        _ => status
    };
    public static string[] NextStatuses(string status) => status switch
    {
        "Pending" => ["Confirmed", "Cancelled"],
        "Confirmed" => ["Shipping", "Cancelled"],
        "Shipping" => ["Delivered"],
        _ => []
    };
    public static string ActionName(string status) => status switch
    {
        "Confirmed" => "Xác nhận đơn hàng", "Shipping" => "Bắt đầu giao hàng", "Delivered" => "Xác nhận đã giao", "Cancelled" => "Hủy đơn hàng", _ => status
    };
}
public sealed class BuyerOrderItem
{
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string Options { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
public sealed record OrderEvent(string Status, DateTimeOffset At);
