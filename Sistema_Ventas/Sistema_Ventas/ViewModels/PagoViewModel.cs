using System.ComponentModel.DataAnnotations;

namespace Sistema_Ventas.ViewModels
{
    public class PagoViewModel
    {
        public int IdReserva { get; set; }
        public decimal Monto { get; set; }
        public string NombreEvento { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string NombreSector { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de tarjeta es obligatorio.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "La tarjeta debe tener exactamente 16 dígitos.")]
        public string NumeroTarjeta { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del titular es obligatorio.")]
        [StringLength(100)]
        public string NombreTitular { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Formato inválido. Use MM/AA.")]
        public string FechaVencimiento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El CVV es obligatorio.")]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "El CVV debe tener 3 dígitos.")]
        public string CVV { get; set; } = string.Empty;
    }
}