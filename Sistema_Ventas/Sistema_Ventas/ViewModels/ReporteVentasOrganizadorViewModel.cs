using Sistema_Ventas.Models;

namespace Sistema_Ventas.ViewModels
{
    public class ReporteVentasOrganizadorViewModel
    {
        public List<Evento> Eventos { get; set; } = new();

        public int? IdEventoSeleccionado { get; set; }

        public string NombreEvento { get; set; } = string.Empty;

        public DateTime? FechaEvento { get; set; }

        public int EntradasVendidas { get; set; }

        public decimal RecaudacionTotal { get; set; }

        public List<ReporteSectorViewModel> Sectores { get; set; } = new();
    }

    public class ReporteSectorViewModel
    {
        public string NombreSector { get; set; } = string.Empty;

        public int Capacidad { get; set; }

        public int EntradasVendidas { get; set; }

        public decimal PorcentajeOcupacion =>
            Capacidad > 0
                ? Math.Round((decimal)EntradasVendidas * 100 / Capacidad, 2)
                : 0;
    }
}
