using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Packt.Shared;

public static class NorthwindContextExtensions
{
    public static IServiceCollection AddNorthwindContext(
        this IServiceCollection services, string relativePath = "..")
    {
        string databasePath = Path.Combine(relativePath, "Northwind.db");

        Console.WriteLine($"Data Source={databasePath}");
        
        services.AddDbContext<NorthwindContext>(option =>
          option.UseSqlite($"Data Source={databasePath}")
        );

        return services;
    }
}