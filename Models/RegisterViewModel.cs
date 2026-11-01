using System.ComponentModel.DataAnnotations;

namespace ScoutVision.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ingresá un correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí un nombre de usuario.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 60 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "El usuario solo puede contener letras, números, punto, guion y guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Creá una contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
