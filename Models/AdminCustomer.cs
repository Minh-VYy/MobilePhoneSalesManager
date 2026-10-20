namespace QuanLyBanDienThoai.Models;

// Contact projection deliberately excludes authentication fields.
public sealed record AdminCustomer(string Id, string Username, string FullName, string Email, string Phone, string Address, DateTimeOffset? CreatedAt);
public sealed record CustomerSummary(AdminCustomer Customer, int OrderCount, int PendingCount, decimal DeliveredTotal, DateTimeOffset? LastOrderAt);
public sealed record CustomerDetailsModel(CustomerSummary Summary, List<BuyerOrder> Orders, bool HasConversation);
public sealed record DataFileStatus(string Key, string Name, int Count, long Bytes, DateTimeOffset? UpdatedAt, ValidationResultModel Validation);
