using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// NotificationService backs an in-process queue and must be shared across requests,
// so it is registered as a singleton (replaces the MSMQ-backed implementation, which
// has no supported .NET 10 equivalent).
builder.Services.AddSingleton<NotificationService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Preserve the legacy ASP.NET MVC 5 JsonResult behavior (PascalCase property
        // names) so the existing Scripts/notifications.js client code keeps working
        // without changes (it reads notification.Operation, notification.EntityType, etc.).
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize/seed the database on startup (replaces Global.asax.cs Application_Start).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

app.Run();
