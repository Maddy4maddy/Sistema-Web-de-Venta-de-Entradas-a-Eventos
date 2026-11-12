
namespace Sistema_Ventas.Models
{
    public class Evento
    {
        public int IdEvento { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public string Lugar { get; set; } = string.Empty;

        public string Ciudad { get; set; } = string.Empty;

        public string Imagen { get; set; } = string.Empty;

        public decimal PrecioInicial { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
