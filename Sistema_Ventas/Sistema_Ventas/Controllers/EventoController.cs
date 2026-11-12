
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sistema_Ventas.Services;

namespace Sistema_Ventas.Controllers
{
    public class EventoController : Controller
    {
        private readonly EventoService eventoService;

        public EventoController(EventoService eventoService)
        {
            this.eventoService = eventoService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Cartelera(
            string? nombre,
            string? categoria,
            DateTime? fecha,
            string? ciudad)
        {
            var eventos = await eventoService.ObtenerEventosActivos(
                nombre,
                categoria,
                fecha,
                ciudad
            );

            ViewBag.Nombre = nombre;
            ViewBag.Categoria = categoria;
            ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");
            ViewBag.Ciudad = ciudad;

            return View(eventos);
        }
    }
}
