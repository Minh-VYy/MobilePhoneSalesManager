using QuanLyBanDienThoai.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register XML & XSLT Services
builder.Services.AddSingleton<XmlValidationService>();
builder.Services.AddTransient<DienThoaiXmlService>();
builder.Services.AddTransient<HoaDonXmlService>();
builder.Services.AddTransient<XsltTransformService>();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=DienThoai}/{action=Index}/{id?}");

app.Run();
