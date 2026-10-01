namespace PSAcademyBack.Dtos.Categories;

public class CategoryResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int OrderIndex { get; set; }

    /// <summary>Número de ejercicios activos de la categoría.</summary>
    public int ExerciseCount { get; set; }
}
