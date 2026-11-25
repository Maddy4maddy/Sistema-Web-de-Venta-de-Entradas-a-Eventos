using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Services;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class HistorialController : Controller
    {
        private readonly HistorialService historialService;

        public HistorialController(HistorialService historialService)
        {
            this.historialService = historialService;
        }

        [HttpGet]
        public async Task<IActionResult> MisCompras()
        {
            string? textoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(textoId, out int idUsuario))
            {
                return Forbid();
            }

            var compras = await historialService.ObtenerComprasPorUsuario(idUsuario);
            return View(compras);
        }
    }
}