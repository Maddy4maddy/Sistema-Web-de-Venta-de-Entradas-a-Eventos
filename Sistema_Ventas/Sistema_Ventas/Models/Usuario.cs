using Microsoft.AspNetCore.Identity;


namespace Sistema_Ventas.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string Contrasena { get; set; } = string.Empty;

        public int IdRol { get; set; }

        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; }

        public string NombreRol { get; set; } = string.Empty;
    }
}

