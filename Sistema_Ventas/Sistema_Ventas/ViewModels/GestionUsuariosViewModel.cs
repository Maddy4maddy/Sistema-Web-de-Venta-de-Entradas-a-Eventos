using Sistema_Ventas.Models;

namespace Sistema_Ventas.ViewModels
{
    public class GestionUsuariosViewModel
    {
        public List<Usuario> Usuarios { get; set; } = new();
        public List<Rol> Roles { get; set; } = new();
    }
}
