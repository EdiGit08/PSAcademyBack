using PSAcademyBack.Enums;

namespace PSAcademyBack.Dtos.Exercises;

public class ExerciseDetailResponse
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Difficulty Difficulty { get; set; }

    public string ExpectedOutput { get; set; } = string.Empty;

    public string? UserStatus { get; set; }

    public List<ExerciseTemplateResponse> Templates { get; set; } = new();

    /// <summary>Valores de entrada que el programa recibirá por stdin, en orden.</summary>
    public List<ExerciseInputResponse> Inputs { get; set; } = new();

    /// <summary>
    /// Código guardado por el alumno, por lenguaje. Solo se rellena con JWT válido;
    /// permite continuar donde se dejó aunque no se haya ejecutado.
    /// </summary>
    public List<ExerciseDraftResponse> Drafts { get; set; } = new();
}
