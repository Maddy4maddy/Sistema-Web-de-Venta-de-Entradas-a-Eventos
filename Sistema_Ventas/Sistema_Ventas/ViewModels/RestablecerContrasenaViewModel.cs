using System.ComponentModel.DataAnnotations;

namespace Sistema_Ventas.ViewModels
{
    public class RestablecerContrasenaViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [StringLength(254)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código de recuperación es obligatorio.")]
        [StringLength(6, MinimumLength = 6,
            ErrorMessage = "El código debe tener 6 dígitos.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(72, MinimumLength = 8,
            ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
        [DataType(DataType.Password)]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare(nameof(NuevaContrasena),
            ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}