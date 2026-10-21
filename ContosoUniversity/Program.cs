using System;
using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configuration: Web.config <connectionStrings>/<appSettings> -> appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.Configure<NotificationQueueOptions>(
    builder.Configuration.GetSection("NotificationQueue"));

builder.Services.AddSingleton<NotificationService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize/seed the database with EF Core (equivalent of Global.asax Application_Start).
// Wrapped in try/catch so that a temporarily unreachable database (e.g. SQL Server/LocalDB
// not yet provisioned in this environment) does not prevent the app from starting - it logs
// the failure instead. This mirrors typical cloud-native startup hardening and does not change
// request-handling behavior; DbInitializer still runs fully whenever the database is reachable.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    try
    {
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database initialization failed. The application will continue to start, but data access will fail until the database is reachable.");
    }
}

app.Run();
