using System;
using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    // Preserve the original ASP.NET MVC 5 JsonResult behavior (JavaScriptSerializer preserves
    // CLR property names) so that Scripts/notifications.js — which reads PascalCase properties
    // such as notification.CreatedAt / notification.Message — keeps working unchanged.
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<MessageQueueOptions>(
    builder.Configuration.GetSection("NotificationQueue"));

// NotificationService owns an MSMQ MessageQueue handle for its lifetime — register as scoped so
// each request (and BaseController, which disposes it) gets its own instance, matching the
// previous `new NotificationService()` construction in BaseController.
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Seed/migrate the database on startup, equivalent to Global.asax's Application_Start ->
// InitializeDatabase(). Wrapped in try/catch so that an unreachable database (e.g. no SQL
// Server/LocalDB available in the current environment) doesn't prevent the process from starting;
// this matches the original ASP.NET Framework app, which would only fail on first *request* that
// touched the database rather than refusing to start the whole app domain.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database initialization failed. The application will continue to start, but data-dependent pages will fail until a database is reachable.");
    }
}

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

app.Run();
