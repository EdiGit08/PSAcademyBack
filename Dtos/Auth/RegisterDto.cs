using System.ComponentModel.DataAnnotations;

namespace PSAcademyBack.Dtos.Auth;

public class RegisterDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [MaxLength(320, ErrorMessage = "El correo no puede superar los 320 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
        ErrorMessage = "La contraseña debe incluir al menos una minúscula, una mayúscula, un dígito y un símbolo.")]
    public string Password { get; set; } = string.Empty;
}
