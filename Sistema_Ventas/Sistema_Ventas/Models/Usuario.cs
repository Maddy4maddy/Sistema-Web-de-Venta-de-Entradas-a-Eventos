using Microsoft.AspNetCore.Identity;

namespace Sistema_Ventas.Models
{
    public class Usuario : IdentityUser
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
