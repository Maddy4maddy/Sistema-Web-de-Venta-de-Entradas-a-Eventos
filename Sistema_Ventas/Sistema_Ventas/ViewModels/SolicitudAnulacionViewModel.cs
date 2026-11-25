using System.ComponentModel.DataAnnotations;

namespace Sistema_Ventas.ViewModels
{
    public class SolicitudAnulacionViewModel
    {
        public int IdCompra { get; set; }

        [Required(ErrorMessage = "Escribe el motivo de la anulación.")]
        [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
        public string Motivo { get; set; } = string.Empty;
    }
}
