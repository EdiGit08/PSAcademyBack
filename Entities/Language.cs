namespace PSAcademyBack.Entities;

public class Language
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Identificador estable usado por el cliente, p. ej. "python", "java", "pseint".</summary>
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<ExerciseTemplate> Templates { get; set; } = new List<ExerciseTemplate>();

    public ICollection<UserCodeDraft> Drafts { get; set; } = new List<UserCodeDraft>();

    public ICollection<UserProgress> Progress { get; set; } = new List<UserProgress>();
}
