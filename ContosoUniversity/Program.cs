using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Preserve the original Web.config httpRuntime limits (maxRequestLength="10240" KB,
// executionTimeout="3600" seconds) and requestFiltering maxAllowedContentLength (10485760 bytes).
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // ~10 MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // ~10 MB
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// EF Core database context (replaces SchoolContextFactory / ConfigurationManager usage).
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Notification queue configuration (replaces appSettings["NotificationQueuePath"]).
builder.Services.Configure<MessageQueueOptions>(
    builder.Configuration.GetSection("MessageQueue"));
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Seed/initialize the database (replaces Global.asax.cs Application_Start -> InitializeDatabase).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
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
