using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QuanLyBanDienThoai.Models;

namespace QuanLyBanDienThoai.Services;

public sealed class AccountXmlService
{
    private readonly PrivateXmlStore _store;
    private readonly PasswordHasher<AccountRecord> _hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
    private readonly string _dummyHash;
    public AccountXmlService(PrivateXmlStore store)
    {
        _store = store;
        _dummyHash = _hasher.HashPassword(new(), Guid.NewGuid().ToString());
        lock (PrivateXmlStore.Gate)
        {
            if (!HasAdmin && !File.Exists(SetupKeyPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SetupKeyPath)!);
                File.WriteAllText(SetupKeyPath, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            }
        }
    }
    public string SetupKeyPath => _store.PathFor("AdminSetup.key");
    public bool HasAdmin => _store.Read("Accounts.xml", "Accounts", root => root.Elements("Account").Any(x => (string?)x.Element("Role") == "Admin"));
    public int CustomerCount => _store.Read("Accounts.xml", "Accounts", root => root.Elements("Account").Count(x => (string?)x.Element("Role") == "User"));
    public List<AdminCustomer> Customers() => _store.Read("Accounts.xml", "Accounts", root => root.Elements("Account")
        .Where(x => (string?)x.Element("Role") == "User").Select(x => new AdminCustomer(
            (string?)x.Attribute("id") ?? "", (string?)x.Element("Username") ?? "", (string?)x.Element("FullName") ?? "",
            (string?)x.Element("Email") ?? "", (string?)x.Element("Phone") ?? "", (string?)x.Element("Address") ?? "",
            DateTimeOffset.TryParse((string?)x.Element("CreatedAt"), out var created) ? created : null)).OrderByDescending(x => x.CreatedAt).ToList());
    public AccountRecord? Find(string? id) => _store.Read("Accounts.xml", "Accounts", root => root.Elements("Account").Where(x => (string?)x.Attribute("id") == id).Select(Map).FirstOrDefault());

    public AccountRecord? Authenticate(string username, string password) => _store.Update("Accounts.xml", "Accounts", root =>
    {
        var element = root.Elements("Account").FirstOrDefault(x => Equal(x.Element("Username")?.Value, username) || Equal(x.Element("Email")?.Value, username));
        if (element == null) { _hasher.VerifyHashedPassword(new(), _dummyHash, password); return null; }
        var account = Map(element);
        if (DateTimeOffset.TryParse((string?)element.Element("LockedUntil"), out var until) && until > DateTimeOffset.UtcNow) return null;
        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            var failures = ((int?)element.Element("Failures") ?? 0) + 1;
            element.SetElementValue("Failures", failures >= 5 ? 0 : failures);
            if (failures >= 5) element.SetElementValue("LockedUntil", DateTimeOffset.UtcNow.AddMinutes(15).ToString("O"));
            return null;
        }
        element.SetElementValue("Failures", 0);
        element.Element("LockedUntil")?.Remove();
        if (result == PasswordVerificationResult.SuccessRehashNeeded) element.SetElementValue("PasswordHash", _hasher.HashPassword(account, password));
        return account;
    });

    public string? Register(RegisterInput input, bool admin = false, string? setupKey = null)
    {
        lock (PrivateXmlStore.Gate)
        {
            if (admin && (HasAdmin || !File.Exists(SetupKeyPath) || !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(setupKey ?? "")),
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SetupKeyPath).Trim()))))) return "Mã khởi tạo không hợp lệ hoặc quản trị viên đã được tạo.";
            var error = _store.Update<string?>("Accounts.xml", "Accounts", root =>
            {
                if (root.Elements("Account").Any(x => Equal(x.Element("Username")?.Value, input.Username) || Equal(x.Element("Email")?.Value, input.Email)))
                    return "Tên đăng nhập hoặc email đã được sử dụng.";
                var account = new AccountRecord { Id = Guid.NewGuid().ToString("N") };
                root.Add(new XElement("Account", new XAttribute("id", account.Id),
                    new XElement("Username", input.Username.Trim()), new XElement("FullName", input.FullName.Trim()),
                    new XElement("Email", input.Email.Trim()), new XElement("Phone", input.Phone.Trim()), new XElement("Address", (input.Address ?? "").Trim()),
                    new XElement("Role", admin ? "Admin" : "User"), new XElement("SecurityStamp", Guid.NewGuid().ToString("N")),
                    new XElement("PasswordHash", _hasher.HashPassword(account, input.Password)), new XElement("CreatedAt", DateTimeOffset.UtcNow.ToString("O"))));
                return null;
            });
            if (error == null && admin) File.Delete(SetupKeyPath);
            return error;
        }
    }

    public string? UpdateProfile(string id, ProfileInput input) => _store.Update<string?>("Accounts.xml", "Accounts", root =>
    {
        var account = root.Elements("Account").First(x => (string?)x.Attribute("id") == id);
        if (root.Elements("Account").Any(x => x != account && Equal(x.Element("Email")?.Value, input.Email))) return "Email đã được sử dụng.";
        account.SetElementValue("FullName", input.FullName.Trim()); account.SetElementValue("Email", input.Email.Trim());
        account.SetElementValue("Phone", input.Phone.Trim()); account.SetElementValue("Address", (input.Address ?? "").Trim());
        return null;
    });

    public bool ChangePassword(string id, string currentPassword, string password) => _store.Update("Accounts.xml", "Accounts", root =>
    {
        var element = root.Elements("Account").First(x => (string?)x.Attribute("id") == id);
        var account = Map(element);
        if (_hasher.VerifyHashedPassword(account, account.PasswordHash, currentPassword) == PasswordVerificationResult.Failed) return false;
        element.SetElementValue("PasswordHash", _hasher.HashPassword(account, password));
        element.SetElementValue("SecurityStamp", Guid.NewGuid().ToString("N"));
        return true;
    });

    private static bool Equal(string? left, string right) => string.Equals(left, right.Trim(), StringComparison.OrdinalIgnoreCase);
    private static AccountRecord Map(XElement x) => new()
    {
        Id = (string?)x.Attribute("id") ?? "",
        Username = (string?)x.Element("Username") ?? "",
        FullName = (string?)x.Element("FullName") ?? "",
        Email = (string?)x.Element("Email") ?? "",
        Phone = (string?)x.Element("Phone") ?? "",
        Address = (string?)x.Element("Address") ?? "",
        Role = (string?)x.Element("Role") ?? "User",
        PasswordHash = (string?)x.Element("PasswordHash") ?? "",
        SecurityStamp = (string?)x.Element("SecurityStamp") ?? ""
    };
}
