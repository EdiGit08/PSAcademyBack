using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Data;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@psacademy.com";
    private const string DefaultAdminPassword = "Admin123!";

    /// <summary>
    /// Siembra de desarrollo: admin con contraseña conocida + datos de referencia.
    /// Nunca debe invocarse en producción.
    /// </summary>
    public static async Task SeedDevelopmentAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        await SeedAdminAsync(dbContext, logger);
        await SeedReferenceDataAsync(dbContext, logger);
    }

    /// <summary>
    /// Datos de referencia sin datos sensibles: es idempotente y NO crea usuarios,
    /// por lo que es segura en producción.
    ///
    /// Sin filas en "languages" el endpoint /execute siempre respondería 400,
    /// porque valida el slug contra esa tabla; sin "categories" el catálogo del
    /// alumno quedaría vacío. Esta siembra es la razón por la que el primer
    /// arranque en Render deja la aplicación usable sin intervención manual.
    ///
    /// Los 30 ejercicios del primer entregable NO se siembran aquí a propósito:
    /// se dan de alta desde el panel de administración para no acoplar el
    /// contenido del curso al esquema de la API.
    /// </summary>
    public static async Task SeedReferenceDataAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        await SeedLanguagesAsync(dbContext, logger);
        await SeedCategoriesAsync(dbContext, logger);
    }

    private static async Task SeedAdminAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var email = DefaultAdminEmail;

        var adminExists = await dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == email);

        if (adminExists)
        {
            return;
        }

        dbContext.Users.Add(new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword, workFactor: 11),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        logger.LogWarning(
            "Usuario admin de desarrollo creado ({Email}). Cambie esta contraseña fuera de desarrollo.",
            email);
    }

    /// <summary>
    /// Sin filas en languages el endpoint /execute siempre respondería 400,
    /// porque valida el slug contra esta tabla.
    ///
    /// El primer entregable expone únicamente Python, Java y PSeint. Los demás
    /// lenguajes se mantienen en la tabla (sus plantillas históricas siguen vivas)
    /// pero se marcan inactivos para que dejen de aparecer a los alumnos.
    /// </summary>
    private static async Task SeedLanguagesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var supported = new[]
        {
            new Language { Name = "Python", Slug = "python", IsActive = true },
            new Language { Name = "Java", Slug = "java", IsActive = true },
            new Language { Name = "PSeint", Slug = "pseint", IsActive = true }
        };

        var supportedSlugs = supported.Select(l => l.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await dbContext.Languages.ToListAsync();

        var missing = supported
            .Where(l => !existing.Any(e => string.Equals(e.Slug, l.Slug, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missing.Count > 0)
        {
            dbContext.Languages.AddRange(missing);
        }

        // Desactiva cualquier lenguaje fuera del alcance del entregable.
        foreach (var language in existing.Where(l => !supportedSlugs.Contains(l.Slug) && l.IsActive))
        {
            language.IsActive = false;
        }

        if (missing.Count > 0 || dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation(
                "Lenguajes iniciales sembrados: {Added}. Lenguajes fuera de alcance desactivados.",
                missing.Count);
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var categories = new[]
        {
            // OrderIndex 0: el tutorial es la puerta de entrada, asi que aparece antes
            // que cualquier otra categoria en el panel del alumno.
            new Category { Name = "Tutorial", Description = "Aprende a escribir tus primeros programas paso a paso.", OrderIndex = 0 },
            new Category { Name = "Fundamentos", Description = "Variables, tipos y operadores.", OrderIndex = 1 },
            new Category { Name = "Estructuras de control", Description = "Condicionales y bucles.", OrderIndex = 2 },
            new Category { Name = "Funciones", Description = "Definición y reutilización de funciones.", OrderIndex = 3 }
        };

        var existingNames = await dbContext.Categories
            .Select(c => c.Name)
            .ToListAsync();

        var missing = categories
            .Where(c => !existingNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.Categories.AddRange(missing);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Categorías iniciales sembradas: {Count}", missing.Count);
    }
}
