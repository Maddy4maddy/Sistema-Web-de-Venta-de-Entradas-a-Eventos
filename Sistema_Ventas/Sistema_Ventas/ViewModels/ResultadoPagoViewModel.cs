namespace Sistema_Ventas.ViewModels
{
    public class ResultadoPagoViewModel
    {
        public string Estado { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public int IdReserva { get; set; }
        public List<string> CodigosEntradas { get; set; } = new List<string>();
    }
}
