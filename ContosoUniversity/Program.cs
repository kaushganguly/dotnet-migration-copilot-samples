using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Preserve the original Web.config <httpRuntime maxRequestLength="10240" executionTimeout="3600" />
// and <requestLimits maxAllowedContentLength="10485760" /> settings (10 MB request body, 1 hour
// execution timeout) used for teaching-material uploads in CoursesController. See
// TEACHING_MATERIAL_UPLOAD.md for the application-level 5 MB file-size check, which remains the
// binding constraint for end users; these are the outer server-level limits.
const long MaxUploadRequestBodyBytes = 10_485_760; // 10 MB, matches old maxAllowedContentLength
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadRequestBodyBytes;
    options.Limits.KeepAliveTimeout = TimeSpan.FromHours(1); // matches old executionTimeout="3600"
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadRequestBodyBytes;
});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Preserve the original MVC 5 JsonResult behavior (JavaScriptSerializer),
        // which serialized declared CLR member names as-is (PascalCase) with no
        // naming-policy rewriting. This keeps Scripts/notifications.js (the only
        // consumer of the JSON endpoints in ContosoUniversity/Controllers/
        // NotificationsController.cs) working unchanged, since it reads fields
        // such as `notification.Operation`, `notification.CreatedAt`, etc.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// NotificationService owns an in-process notification queue (see Services/NotificationService.cs)
// that must survive across requests, so it is registered as a singleton.
builder.Services.AddSingleton<NotificationService>();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Equivalent of the original Global.asax.cs Application_Start -> InitializeDatabase()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

app.Run();
