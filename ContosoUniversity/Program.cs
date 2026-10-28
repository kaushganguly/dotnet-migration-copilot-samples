using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ContosoUniversity.Data;
using ContosoUniversity.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration: Web.config appSettings/connectionStrings -> appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// EF Core DbContext registered via DI (replaces manual SchoolContextFactory construction)
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(connectionString));

// Notification queue (replaces System.Messaging/MSMQ with a cross-platform in-process queue)
builder.Services.AddSingleton<NotificationService>();

// MVC services. Preserve PascalCase JSON casing for NotificationsController's
// JsonResult endpoints, which are consumed by wwwroot/js/notifications.js
// (data.success, notification.Operation, notification.CreatedAt, etc.) — the
// original System.Web.Mvc Json() helper did not camelCase property names.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

// Database initialization (replaces Global.asax.cs Application_Start/InitializeDatabase)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        logger.LogWarning(ex, "Database initialization was skipped because the database is unavailable.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Replaces App_Start/RouteConfig.cs RegisterRoutes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
