using PSAcademyBack.Enums;

namespace PSAcademyBack.Dtos.Admin;

/// <summary>
/// Ficha de una cuenta vista desde el panel de administración.
///
/// Los contadores de progreso y borradores se incluyen porque al borrar una cuenta
/// el admin necesita saber qué se lleva por delante: un alumno con 12 ejercicios
/// superados y 4 borradores no es lo mismo que una cuenta recién creada, y la
/// operación no tiene vuelta atrás.
/// </summary>
public class AdminUserResponse
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Ejercicios intentados alguna vez (los borrados también cuentan).</summary>
    public int AttemptedCount { get; set; }

    /// <summary>Ejercicios superados.</summary>
    public int CompletedCount { get; set; }

    /// <summary>Borradores de código guardados en el workspace.</summary>
    public int DraftCount { get; set; }

    /// <summary>Última vez que el usuario mandó código o guardó un borrador.</summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// True si la fila es la cuenta con la que el admin está autenticado. La UI lo usa
    /// para deshabilitar las acciones que la API de todas formas va a rechazar, y sobre
    /// todo para no dejarle borrar su propia sesión por accidente.
    /// </summary>
    public bool IsSelf { get; set; }
}
