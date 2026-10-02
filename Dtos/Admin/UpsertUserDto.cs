using System.ComponentModel.DataAnnotations;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Dtos.Admin;

/// <summary>
/// Edición de una cuenta desde el panel de administración.
///
/// Va en su propio archivo (y no en <c>UpsertDtos.cs</c>) porque no edita contenido
/// del curso sino personas: mezclado con los DTO de ejercicios era fácil confundir
/// qué endpoint protege qué.
///
/// La contraseña es opcional a propósito: si no llega, el usuario conserva la suya. El
/// admin nunca ve el hash, así que no puede "reenviarla"; solo puede fijar una nueva.
/// </summary>
public class UpsertUserDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(320, MinimumLength = 5, ErrorMessage = "El correo debe tener entre 5 y 320 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [EnumDataType(typeof(UserRole), ErrorMessage = "Rol no válido. Valores: User, Admin.")]
    public UserRole Role { get; set; } = UserRole.User;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? NewPassword { get; set; }
}
