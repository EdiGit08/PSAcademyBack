namespace PSAcademyBack.Services;

/// <summary>
/// El envio ya estaba calificado cuando llego la peticion.
///
/// Ocurre cuando dos administradores califican la misma fila a la vez o cuando uno
/// recarga la bandeja e intenta actuar sobre algo que ya se califico. No es un fallo del
/// cliente: ladecision ya existe en la base de datos, asi que la API responde 409.
/// </summary>
public sealed class SubmissionAlreadyGradedException : Exception
{
    public SubmissionAlreadyGradedException(int submissionId)
        : base($"El envío {submissionId} ya fue calificado.")
    {
        SubmissionId = submissionId;
    }

    public int SubmissionId { get; }
}
