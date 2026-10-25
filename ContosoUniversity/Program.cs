using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<MessageQueueOptions>(
    builder.Configuration.GetSection("MessageQueue"));

// Data layer
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<NotificationService>();

// MVC
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        // Preserve the pre-migration JSON wire shape (PascalCase property names,
        // matching the original System.Web.Mvc JsonResult / Json.NET serialization)
        // so the existing same-origin notifications.js client keeps working unchanged.
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

var app = builder.Build();

// Initialize database with EF Core (mirrors legacy Global.asax Application_Start behavior)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
