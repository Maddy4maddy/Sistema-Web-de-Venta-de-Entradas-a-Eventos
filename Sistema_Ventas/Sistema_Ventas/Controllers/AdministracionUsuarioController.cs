using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Services;
using Sistema_Ventas.ViewModels;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministracionUsuarioController : Controller
    {
        private readonly AdministracionUsuarioService administracionService;

        public AdministracionUsuarioController(
            AdministracionUsuarioService administracionService)
        {
            this.administracionService = administracionService;
        }

        [HttpGet]
        public async Task<IActionResult> GestionarUsuarios()
        {
            var modelo = new GestionUsuariosViewModel
            {
                Usuarios = await administracionService.ObtenerUsuarios(),
                Roles = await administracionService.ObtenerRoles()
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int idUsuario, bool activo)
        {
            string? textoIdActual = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!activo && int.TryParse(textoIdActual, out int idActual)
                && idUsuario == idActual)
            {
                TempData["Error"] = "No puedes bloquear tu propia cuenta.";
                return RedirectToAction(nameof(GestionarUsuarios));
            }

            bool actualizado =
                await administracionService.CambiarEstado(idUsuario, activo);

            TempData["Mensaje"] = actualizado
                ? "El estado de la cuenta se actualizó."
                : "No se encontró la cuenta.";

            return RedirectToAction(nameof(GestionarUsuarios));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(int idUsuario, int idRol)
        {
            string? textoIdActual = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(textoIdActual, out int idActual)
                && idUsuario == idActual)
            {
                TempData["Error"] = "No puedes cambiar tu propio rol.";
                return RedirectToAction(nameof(GestionarUsuarios));
            }

            bool actualizado =
                await administracionService.CambiarRol(idUsuario, idRol);

            TempData["Mensaje"] = actualizado
                ? "El rol de la cuenta se actualizó."
                : "No se encontró la cuenta.";

            return RedirectToAction(nameof(GestionarUsuarios));
        }
    }
}
