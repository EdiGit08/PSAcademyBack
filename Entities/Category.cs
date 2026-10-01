namespace PSAcademyBack.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int OrderIndex { get; set; }

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
