using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sistema_Ventas.Services;
using Sistema_Ventas.ViewModels;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    public class EventoController : Controller
    {
        private readonly EventoService eventoService;
        private readonly ReservaService reservaService;
        private readonly PagoService pagoService;

        public EventoController(EventoService eventoService, ReservaService reservaService, PagoService pagoService)
        {
            this.eventoService = eventoService;
            this.reservaService = reservaService;
            this.pagoService = pagoService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Cartelera(string? nombre, string? categoria, DateTime? fecha, string? ciudad)
        {
            var eventos = await eventoService.ObtenerEventosActivos(nombre, categoria, fecha, ciudad);
            ViewBag.Nombre = nombre;
            ViewBag.Categoria = categoria;
            ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");
            ViewBag.Ciudad = ciudad;
            return View(eventos);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Detalle(int id)
        {
            var evento = await eventoService.ObtenerEventoPorId(id);
            if (evento == null) return NotFound();

            var sectores = await eventoService.ObtenerSectoresPorEvento(id);

            var modelo = new ReservaViewModel
            {
                IdEvento = evento.IdEvento,
                NombreEvento = evento.Nombre,
                Sectores = sectores
            };

            // Verificar si el usuario ya tiene una reserva pendiente
            int? idReservaPendiente = null;
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Cliente"))
            {
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                idReservaPendiente = await reservaService.ObtenerReservaPendiente(idUsuario);
            }
            ViewBag.IdReservaPendiente = idReservaPendiente;

            ViewBag.Evento = evento;
            return View(modelo);
        }

        [HttpPost]
        [Authorize(Roles = "Cliente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(ReservaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                var evento = await eventoService.ObtenerEventoPorId(modelo.IdEvento);
                modelo.Sectores = await eventoService.ObtenerSectoresPorEvento(modelo.IdEvento);
                ViewBag.Evento = evento;
                return View("Detalle", modelo);
            }

            try
            {
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                int idReserva = await reservaService.CrearReserva(idUsuario, modelo.IdSector, modelo.Cantidad);

                return RedirectToAction("Pagar", "Pago", new { idReserva = idReserva });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var evento = await eventoService.ObtenerEventoPorId(modelo.IdEvento);
                modelo.Sectores = await eventoService.ObtenerSectoresPorEvento(modelo.IdEvento);
                ViewBag.Evento = evento;
                return View("Detalle", modelo);
            }
        }
    }
}