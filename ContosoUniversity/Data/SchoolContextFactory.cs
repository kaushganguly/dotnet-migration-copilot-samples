using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ContosoUniversity.Data
{
    public sealed class SchoolContextFactory : IDesignTimeDbContextFactory<SchoolContext>
    {
        public SchoolContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("The DefaultConnection connection string is missing.");
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new SchoolContext(options);
        }
    }
}
