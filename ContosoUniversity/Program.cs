using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Preserve the legacy Web.config request-size limit (maxAllowedContentLength=10485760 / maxRequestLength=10240KB).
const long MaxRequestBodySizeBytes = 10 * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxRequestBodySizeBytes;
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Keep the PascalCase JSON payload the notifications.js client already expects.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SchoolContext>(options => options.UseSqlServer(connectionString));

// Replaces the former MSMQ-backed queue (System.Messaging is Windows-only and unavailable on .NET 10);
// a singleton keeps notifications shared across requests the same way the named MSMQ queue did.
builder.Services.AddSingleton<NotificationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

app.Run();
