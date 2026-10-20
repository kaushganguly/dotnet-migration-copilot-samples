using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Design-time factory used by EF Core tooling (e.g. `dotnet ef migrations add`)
    /// to construct a <see cref="SchoolContext"/> outside of the ASP.NET Core host.
    /// At runtime the app registers <see cref="SchoolContext"/> via dependency injection
    /// in Program.cs instead of using this factory.
    /// </summary>
    public class SchoolContextFactory : IDesignTimeDbContextFactory<SchoolContext>
    {
        public SchoolContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<SchoolContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SchoolContext(optionsBuilder.Options);
        }
    }
}
