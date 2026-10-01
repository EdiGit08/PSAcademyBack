namespace PSAcademyBack.Services;

/// <summary>
/// Ajustes de TLS/HSTS para despliegues que ya terminan TLS en un proxy.
///
/// En producción normal (Render) esto se deja activado: las cabeceras
/// "X-Forwarded-Proto" hacen que la petición siga viéndose como HTTPS.
/// Se desactiva solo en despliegues sin TLS (proxy local en pruebas).
/// </summary>
public class HttpsOptions
{
    public const string SectionName = "Https";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Puerto al que redirigir las peticiones que llegan sin TLS.
    ///
    /// Sin valor, UseHttpsRedirection no puede decidir el destino: registra
    /// "Failed to determine the https port for redirect" y deja servir la
    /// petición tal cual, sin redirigir. Fijarlo convierte ese silencio en una
    /// redirección real.
    ///
    /// 443 es el puerto público de Render y de cualquier proxy que termine TLS
    /// delante. Si el contenedor se expone con otro puerto HTTPS, cambiarlo aquí.
    /// </summary>
    public int HttpsPort { get; set; } = 443;
}
