using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Services;
using Sistema_Ventas.ViewModels;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class AnulacionController : Controller
    {
        private readonly AnulacionService anulacionService;
        private readonly ILogger<AnulacionController> logger;

        public AnulacionController(AnulacionService anulacionService, ILogger<AnulacionController> logger)
        {
            this.anulacionService = anulacionService;
            this.logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Solicitar(SolicitudAnulacionViewModel modelo)
        {
            string? textoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(textoId, out int idUsuario))
            {
                return Forbid();
            }

            modelo.Motivo = modelo.Motivo?.Trim() ?? string.Empty;
            if (!ModelState.IsValid || modelo.IdCompra <= 0 || modelo.Motivo.Length == 0)
            {
                TempData["ErrorAnulacion"] = "Escribe un motivo válido de hasta 500 caracteres.";
                return RedirectToAction("MisCompras", "Historial");
            }

            try
            {
                bool aprobada = await anulacionService.Solicitar(modelo.IdCompra, idUsuario, modelo.Motivo);
                TempData[aprobada ? "ExitoAnulacion" : "ErrorAnulacion"] = aprobada
                    ? "Compra anulada. Se registró el reembolso simulado por el total pagado."
                    : "No es posible anular esta compra. Verifica que sea tuya, que el evento no haya comenzado y que todas las entradas sigan sin usar.";
            }
            catch (Exception error)
            {
                logger.LogError("No se pudo procesar la anulación. Tipo: {TipoError}.", error.GetType().Name);
                TempData["ErrorAnulacion"] = "No pudimos procesar la anulación. Inténtalo más tarde.";
            }

            return RedirectToAction("MisCompras", "Historial");
        }
    }
}
