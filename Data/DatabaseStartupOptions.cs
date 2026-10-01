namespace PSAcademyBack.Data;

/// <summary>
/// Controla qué hace la aplicación con el esquema y los datos al arrancar.
///
/// En desarrollo todo se aplica siempre. Fuera de desarrollo depende de estas
/// banderas, que se pueden fijar como "Database__ApplyMigrationsOnStartup" y
/// "Database__SeedReferenceDataOnStartup" desde el panel de Render.
/// </summary>
public class DatabaseStartupOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Aplica las migraciones pendientes al arrancar. Es idempotente, por lo que
    /// activarlo en producción es seguro.
    ///
    /// Con varias instancias desplegándose a la vez hay una ventana en la que dos
    /// procesos podrían intentar migrar a la vez. MigrateAsync usa un bloqueo de
    /// bloqueo en PostgreSQL, así que el segundo espera y luego detecta que ya no
    /// hay nada pendiente; no es motivo para desactivar la opción.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>
    /// Siembra idiomas y categorías tras migrar. Sin filas en "languages" el
    /// endpoint /execute responde 400 siempre, porque valida el slug contra esa
    /// tabla, así que esta opción debe ir activada en un despliegue nuevo.
    /// </summary>
    public bool SeedReferenceDataOnStartup { get; set; }

    /// <summary>Timeout por comando SQL, en segundos.</summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>Reintentos de Npgsql para el tráfico normal de peticiones.</summary>
    public int MaxRetryCount { get; set; } = 6;

    /// <summary>Espera máxima entre esos reintentos, en segundos.</summary>
    public int RetryDelaySeconds { get; set; } = 10;

    /// <summary>Intentos totales al aplicar las migraciones durante el arranque.</summary>
    public int MigrationMaxAttempts { get; set; } = 8;
}
