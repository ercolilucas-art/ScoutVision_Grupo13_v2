using System.ComponentModel.DataAnnotations;

namespace ScoutVision.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresá tu usuario.")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
