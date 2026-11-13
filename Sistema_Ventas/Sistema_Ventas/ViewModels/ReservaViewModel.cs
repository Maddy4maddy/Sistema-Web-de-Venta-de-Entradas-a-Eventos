using System.ComponentModel.DataAnnotations;
using Sistema_Ventas.Models;

namespace Sistema_Ventas.ViewModels
{
    public class ReservaViewModel
    {
        public int IdEvento { get; set; }
        public string NombreEvento { get; set; } = string.Empty;
        public List<Sector> Sectores { get; set; } = new List<Sector>();

        [Required(ErrorMessage = "Seleccione un sector.")]
        public int IdSector { get; set; }

        [Required(ErrorMessage = "Ingrese la cantidad.")]
        [Range(1, 6, ErrorMessage = "Solo puede comprar entre 1 y 6 entradas.")]
        public int Cantidad { get; set; }
    }
}