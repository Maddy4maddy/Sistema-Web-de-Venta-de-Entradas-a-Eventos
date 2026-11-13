namespace Sistema_Ventas.Models
{
    public class Sector
    {
        public int IdSector { get; set; }
        public int IdEvento { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Capacidad { get; set; }
        public int Disponibles { get; set; } 
    }
}