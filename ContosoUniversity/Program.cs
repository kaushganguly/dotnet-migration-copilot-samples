using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Equivalent of the classic FilterConfig.RegisterGlobalFilters: the original only registered
// HandleErrorAttribute (no authorization filter - the application has no authentication).
// In ASP.NET Core, unhandled-exception handling is provided by the UseExceptionHandler
// middleware configured below, so no MVC filter needs to be registered here.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<MessageQueueOptions>(
    builder.Configuration.GetSection("MessageQueue"));
builder.Services.AddScoped<NotificationService>();

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

// Equivalent of the classic RouteConfig.RegisterRoutes default route.
// (IgnoreRoute("{resource}.axd/{*pathInfo}") has no meaning under Kestrel/ASP.NET Core
// and is intentionally not carried over.)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Equivalent of Global.asax's Application_Start database initialization.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

app.Run();
