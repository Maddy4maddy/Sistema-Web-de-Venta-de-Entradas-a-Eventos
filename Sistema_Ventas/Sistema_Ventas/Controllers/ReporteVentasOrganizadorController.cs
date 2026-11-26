using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Services;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    [Authorize(Roles = "Organizador")]
    public class ReporteVentasOrganizadorController : Controller
    {
        private readonly ReporteVentasOrganizadorService reporteService;

        public ReporteVentasOrganizadorController(
            ReporteVentasOrganizadorService reporteService)
        {
            this.reporteService = reporteService;
        }

        [HttpGet]
        public async Task<IActionResult> VerReporteVentas(int? idEvento)
        {
            string? textoIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(textoIdUsuario, out int idOrganizador))
            {
                return Forbid();
            }

            var modelo = await reporteService.ObtenerReporteVentas(
                idOrganizador,
                idEvento);

            if (modelo == null)
            {
                return NotFound();
            }

            return View(modelo);
        }
    }
}
