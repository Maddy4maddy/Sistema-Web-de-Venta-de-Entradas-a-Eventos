using System.ComponentModel.DataAnnotations;

namespace Sistema_Ventas.ViewModels
{
    public class EventoOrganizadorViewModel
    {
        public int IdEvento { get; set; }

        [Required(ErrorMessage = "Escribe el nombre del evento.")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la categoría.")]
        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indica la fecha del evento.")]
        public DateTime Fecha { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "Escribe el lugar.")]
        [StringLength(200)]
        public string Lugar { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la ciudad.")]
        [StringLength(100)]
        public string Ciudad { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Imagen { get; set; }

        public List<SectorEventoViewModel> Sectores { get; set; } = new()
        {
            new SectorEventoViewModel()
        };
    }

    public class SectorEventoViewModel
    {
        public int IdSector { get; set; }

        [Required(ErrorMessage = "Escribe el nombre del sector.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "9999999999.99",
            ErrorMessage = "El precio debe ser mayor que cero.",
            ParseLimitsInInvariantCulture = true)]
        public decimal Precio { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "El aforo debe ser al menos 1.")]
        public int Capacidad { get; set; }
    }
}
