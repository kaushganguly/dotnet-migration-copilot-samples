using System.IO;
using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// MVC + views, preserving the MVC 5 JSON naming behavior (PascalCase, CLR member names)
// for the existing Notifications JSON endpoints and their in-repo JS consumer.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<MessageQueueOptions>(builder.Configuration.GetSection("MessageQueue"));
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

// Serve the legacy Content/Scripts/Uploads folders from the content root (not wwwroot)
// without moving any files, preserving the existing "~/Content/...", "~/Scripts/...",
// "~/Uploads/..." URLs used throughout the views.
foreach (var folder in new[] { "Content", "Scripts", "Uploads" })
{
    var physicalPath = Path.Combine(app.Environment.ContentRootPath, folder);
    if (Directory.Exists(physicalPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = "/" + folder
        });
    }
}

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

app.Run();
