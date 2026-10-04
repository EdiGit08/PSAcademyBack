namespace PSAcademyBack.Dtos.Exercises;

public class SubmitCodeRequest
{
    public string LanguageSlug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
