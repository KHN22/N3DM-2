using Microsoft.EntityFrameworkCore;
using N3DMMarket.Models.Db;
using N3DMMarket.Filters;

var builder = WebApplication.CreateBuilder(args);

// configure Kestrel to listen on both default ports so the site works whether
// localhost:5000 or 5001 is used. This avoids 'address already in use' errors
// when port 5000 is occupied by another process (e.g. IIS Express).
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
    options.ListenLocalhost(5001);
});

// Add services
builder.Services.AddControllersWithViews();
// register role filter factory for attribute usage
builder.Services.AddScoped<RoleAuthorizeFilter>(sp => Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<RoleAuthorizeFilter>(sp, ""));
// Add session support for cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// Register EF Core DbContext using the connection string from appsettings.json
builder.Services.AddDbContext<ThreedmContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Threedm")));

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// Seed default roles (Admin, Moderator, Seller, Customer)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ThreedmContext>();
    try
    {
        var roles = new[] { "Admin", "Moderator", "Seller", "SellerPending", "SellerRejected", "Customer" };
        foreach (var rn in roles)
        {
            if (!context.Roles.Any(r => r.RoleName == rn))
            {
                context.Roles.Add(new N3DMMarket.Models.Db.Role { RoleName = rn });
            }
        }
        context.SaveChanges();
    }
    catch
    {
        // swallowing exceptions here because seeding should not crash the app during startup
    }
}

app.Run();
