using QuanLyBanDienThoai.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "PhoneStore.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        var account = context.HttpContext.RequestServices.GetRequiredService<AccountXmlService>()
            .Find(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier));
        if (account == null || account.SecurityStamp != context.Principal?.FindFirstValue("SecurityStamp") || account.Role != context.Principal?.FindFirstValue(ClaimTypes.Role))
        { context.RejectPrincipal(); await context.HttpContext.SignOutAsync(); }
    };
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Headers.Accept.ToString().Contains("application/json") || context.Request.Path.StartsWithSegments("/Support/Events")) context.Response.StatusCode = 401;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Headers.Accept.ToString().Contains("application/json")) context.Response.StatusCode = 403;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("chat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "guest", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddSingleton<PrivateXmlStore>();
builder.Services.AddSingleton<AccountXmlService>();
builder.Services.AddSingleton<SupportChatService>();
builder.Services.AddTransient<CustomerOrderService>();

// Register XML & XSLT Services
builder.Services.AddSingleton<XmlValidationService>();
builder.Services.AddTransient<DienThoaiXmlService>();
builder.Services.AddTransient<HoaDonXmlService>();
builder.Services.AddTransient<XsltTransformService>();
builder.Services.AddTransient<XmlOrderService>();
builder.Services.AddTransient<QuangCaoXmlService>();
builder.Services.AddSingleton<PhoneDetailXmlService>();
builder.Services.AddTransient<StockUnitService>();
builder.Services.AddTransient<TinTucXmlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Generate a local, one-time setup key only when no real administrator exists.
_ = app.Services.GetRequiredService<AccountXmlService>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
