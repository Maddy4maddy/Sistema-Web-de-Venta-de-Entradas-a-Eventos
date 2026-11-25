using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Services;
using Sistema_Ventas.ViewModels;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    // Solo los organizadores pueden acceder a validar las entradas.
    [Authorize(Roles = "Organizador")]
    public class ValidacionEntradaController : Controller
    {
        private readonly ValidacionEntradaService validacionEntradaService;

        public ValidacionEntradaController(ValidacionEntradaService validacionEntradaService)
        {
            this.validacionEntradaService = validacionEntradaService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ValidacionEntradaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ValidacionEntradaViewModel modelo)
        {
            modelo.Codigo = modelo.Codigo?.Trim() ?? string.Empty;
            ModelState.Clear();
            if (!TryValidateModel(modelo))
            {
                return View(modelo);
            }

            // Obtiene el identificador de la sesión
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int idOrganizador))
            {
                return Forbid();
            }

            modelo.Validada = await validacionEntradaService.Validar(modelo.Codigo, idOrganizador);
            modelo.Mensaje = modelo.Validada
                ? "Entrada válida. Se registró su uso correctamente."
                : "No se pudo validar esta entrada. Comprueba el código y que corresponda a un evento tuyo vigente.";

            if (modelo.Validada)
            {
                modelo.Codigo = string.Empty;
            }

            return View(modelo);
        }
    }
}
