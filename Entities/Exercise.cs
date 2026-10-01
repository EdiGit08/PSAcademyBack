using PSAcademyBack.Enums;

namespace PSAcademyBack.Entities;

public class Exercise
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    /// <summary>Enunciado en Markdown.</summary>
    public string Description { get; set; } = string.Empty;

    public Difficulty Difficulty { get; set; } = Difficulty.Easy;

    /// <summary>Salida exacta esperada. Se compara con stdout tras normalizar.</summary>
    public string ExpectedOutput { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExerciseTemplate> Templates { get; set; } = new List<ExerciseTemplate>();

    /// <summary>Valores que el programa recibe por stdin ("valores del leer").</summary>
    public ICollection<ExerciseInput> Inputs { get; set; } = new List<ExerciseInput>();

    public ICollection<UserProgress> Progress { get; set; } = new List<UserProgress>();

    public ICollection<UserCodeDraft> Drafts { get; set; } = new List<UserCodeDraft>();
}
