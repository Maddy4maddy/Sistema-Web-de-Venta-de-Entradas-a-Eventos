using System.ComponentModel.DataAnnotations;

namespace Sistema_Ventas.ViewModels
{
    public class ValidacionEntradaViewModel
    {
        [Required(ErrorMessage = "Ingresa el código de la entrada.")]
        [StringLength(50, ErrorMessage = "El código es demasiado largo.")]
        [RegularExpression(@"^ENT-[A-Fa-f0-9]{32}$", ErrorMessage = "Ingresa un código de entrada válido.")]
        public string Codigo { get; set; } = string.Empty;

        public string? Mensaje { get; set; }

        public bool Validada { get; set; }
    }
}
