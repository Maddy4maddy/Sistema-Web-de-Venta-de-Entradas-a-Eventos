using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Services;
using Sistema_Ventas.ViewModels;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    [Authorize(Roles = "Organizador")]
    public class OrganizadorController : Controller
    {
        private readonly OrganizadorEventoService organizadorEventoService;

        public OrganizadorController(
            OrganizadorEventoService organizadorEventoService)
        {
            this.organizadorEventoService = organizadorEventoService;
        }

        [HttpGet]
        public async Task<IActionResult> MisEventos()
        {
            int idOrganizador = ObtenerIdOrganizador();

            var eventos = await organizadorEventoService
                .ObtenerEventosPorOrganizador(idOrganizador);

            return View(eventos);
        }

        [HttpGet]
        public IActionResult CrearEvento()
        {
            return MostrarFormulario(new EventoOrganizadorViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> EditarEvento(int id)
        {
            int idOrganizador = ObtenerIdOrganizador();

            var modelo = await organizadorEventoService
                .ObtenerEventoParaEditar(id, idOrganizador);

            if (modelo == null)
            {
                return NotFound();
            }

            return MostrarFormulario(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEvento(
            EventoOrganizadorViewModel modelo)
        {
            if (modelo.Sectores == null || modelo.Sectores.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.Sectores),
                    "Agrega al menos un sector.");
            }

            if (modelo.Fecha <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(modelo.Fecha),
                    "La fecha del evento debe ser futura.");
            }

            if (!ModelState.IsValid)
            {
                return MostrarFormulario(modelo);
            }

            int idOrganizador = ObtenerIdOrganizador();

            if (modelo.IdEvento == 0)
            {
                await organizadorEventoService.CrearEvento(
                    modelo,
                    idOrganizador);

                TempData["Mensaje"] =
                    "El evento se guardó como borrador. Puedes publicarlo desde Mis eventos.";
            }
            else
            {
                bool actualizado = await organizadorEventoService.ActualizarEvento(
                    modelo,
                    idOrganizador);

                if (!actualizado)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se guardaron los cambios. Revisa los sectores y sus aforos.");

                    return MostrarFormulario(modelo);
                }

                TempData["Mensaje"] = "El evento se actualizó.";
            }

            return RedirectToAction(nameof(MisEventos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublicarEvento(int id)
        {
            int idOrganizador = ObtenerIdOrganizador();

            bool publicado = await organizadorEventoService
                .CambiarEstadoEvento(id, idOrganizador, true);

            TempData[publicado ? "Mensaje" : "Error"] = publicado
                ? "El evento ya está publicado."
                : "No se pudo publicar. Revisa que tenga sectores y una fecha futura.";

            return RedirectToAction(nameof(MisEventos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarEvento(int id)
        {
            int idOrganizador = ObtenerIdOrganizador();

            bool cancelado = await organizadorEventoService
                .CancelarEventoConReembolsos(id, idOrganizador);

            TempData[cancelado ? "Mensaje" : "Error"] = cancelado
                ? "El evento se canceló. Se anularon las reservas y se registraron los reembolsos simulados."
                : "No se encontró el evento.";

            return RedirectToAction(nameof(MisEventos));
        }

        private IActionResult MostrarFormulario(
            EventoOrganizadorViewModel modelo)
        {
            ViewData["Title"] = modelo.IdEvento == 0
                ? "Crear evento"
                : "Editar evento";

            return View("FormularioEvento", modelo);
        }

        private int ObtenerIdOrganizador()
        {
            string? id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(id, out int idOrganizador))
            {
                throw new InvalidOperationException(
                    "No se encontró el identificador del organizador.");
            }

            return idOrganizador;
        }
    }
}
