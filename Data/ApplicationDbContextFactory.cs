using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PSAcademyBack.Data;

namespace PSAcademyBack.Data;

/// <summary>
/// Permite ejecutar "dotnet ef migrations" sin levantar la API. La cadena de conexión
/// se resuelve en este orden: variable de entorno PSACADEMY_CONNECTION, appsettings.json
/// (que se versiona sin contraseña) y, como último recurso, un destino de diseño local
/// sin contraseña que basta para generar migraciones.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DesignTimeFallback =
        "Host=localhost;Port=5433;Database=psacademy_design;Username=postgres";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(ResolveConnectionString());

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("PSACADEMY_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var configured = configuration.GetConnectionString("DefaultConnection");
        return string.IsNullOrWhiteSpace(configured) ? DesignTimeFallback : configured;
    }
}