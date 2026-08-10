using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PassengerService.Data;

namespace PassengerService;

public class PassengerDbContextFactory : IDesignTimeDbContextFactory<PassengerDbContext>
{
    public PassengerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<PassengerDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        optionsBuilder.UseSqlServer(connectionString);

        return new PassengerDbContext(optionsBuilder.Options);
    }
}
