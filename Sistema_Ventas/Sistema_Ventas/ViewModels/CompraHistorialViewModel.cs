namespace Sistema_Ventas.ViewModels
{
    public class CompraHistorialViewModel
    {
        public int IdCompra { get; set; }
        public string NombreEvento { get; set; } = string.Empty;
        public DateTime FechaEvento { get; set; }
        public string NombreSector { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public List<string> CodigosEntradas { get; set; } = new List<string>();
        public DateTime FechaCompra { get; set; }
        public string EstadoCompra { get; set; } = string.Empty;
        public bool EventoFuturo { get; set; }
        public int EntradasEmitidas { get; set; }

        public bool PuedeAnular => EstadoCompra == "Pagada" && EventoFuturo &&
            Cantidad > 0 && CodigosEntradas.Count == Cantidad && EntradasEmitidas == Cantidad;
    }
}
