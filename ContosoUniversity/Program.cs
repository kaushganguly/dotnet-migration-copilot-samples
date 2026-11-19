using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<MessageQueueOptions>(
    builder.Configuration.GetSection("MessageQueue"));

// Services
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<NotificationService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Preserve MVC 5 JsonResult (JavaScriptSerializer) behavior: declared member
        // names are not rewritten. See task.md > Wire Compatibility for the recorded
        // naming-inventory evidence behind this choice.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

var app = builder.Build();

// Seed/initialize the database (replaces Global.asax.cs Application_Start > InitializeDatabase)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/Error");
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
