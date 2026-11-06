using ContosoUniversity.Data;
using ContosoUniversity.Options;
using ContosoUniversity.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration (replaces Web.config connectionStrings/appSettings)
builder.Services.Configure<MessageQueueOptions>(builder.Configuration.GetSection("NotificationQueue"));

// MVC (replaces App_Start/FilterConfig.cs global HandleErrorAttribute equivalent via
// the built-in developer exception page / exception handler middleware configured below)
builder.Services.AddControllersWithViews();

// EF Core DbContext (replaces Data/SchoolContextFactory.cs manual ConfigurationManager-based factory)
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Notification service (MSMQ-backed), replaces manual `new NotificationService()` in BaseController
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

// Replaces App_Start/RouteConfig.cs default conventional route.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed the database on startup (replaces Global.asax.cs Application_Start -> DbInitializer.Initialize)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SchoolContext>();
    try
    {
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database. The application will continue to start, but database-dependent requests may fail.");
    }
}

app.Run();
