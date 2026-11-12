
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Services;

namespace Sistema_Ventas.Controllers
{
    [Route("[controller]")]
    public class conexionController : Controller
    {
        private readonly conexionService servicio;

        public conexionController(conexionService servicio)
        {
            this.servicio = servicio;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string resultado = await servicio.ProbarConexion();

            return Content(resultado);
        }
    }
}
