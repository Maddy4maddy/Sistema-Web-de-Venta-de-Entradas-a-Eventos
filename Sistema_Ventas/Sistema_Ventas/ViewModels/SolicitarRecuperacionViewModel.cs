using System.ComponentModel.DataAnnotations;

namespace Sistema_Ventas.ViewModels
{
    public class SolicitarRecuperacionViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(254)]
        public string Correo { get; set; } = string.Empty;
    }
}