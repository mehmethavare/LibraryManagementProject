using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// MVC
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Library.UI.Filters.AuthFilter());
});

// HttpClient
builder.Services.AddHttpClient();

// Session Servisi
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// API Base URL
builder.Configuration["ApiBaseUrl"] = "https://localhost:7080/api/";

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // 2. Sonra Kimlik Kontrolü
app.UseAuthorization();

// Session Middleware (Routing'den sonra gelmeli)
app.UseSession();


// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();