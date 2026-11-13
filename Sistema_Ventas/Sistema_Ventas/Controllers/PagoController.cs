using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sistema_Ventas.Services;
using Sistema_Ventas.ViewModels;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using System.Data;

namespace Sistema_Ventas.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class PagoController : Controller
    {
        private readonly PagoService pagoService;
        private readonly ReservaService reservaService;
        private readonly ConexionBD conexionBD;

        public PagoController(PagoService pagoService, ReservaService reservaService, ConexionBD conexionBD)
        {
            this.pagoService = pagoService;
            this.reservaService = reservaService;
            this.conexionBD = conexionBD;
        }

        [HttpGet]
        public async Task<IActionResult> Pagar(int idReserva)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            
            await reservaService.LiberarReservasExpiradas(idUsuario);

            if (!await EsReservaDelUsuario(idReserva, idUsuario))
            {
                TempData["Error"] = "No tienes permiso para pagar esta reserva.";
                return RedirectToAction("Cartelera", "Evento");
            }

            var resumen = await pagoService.ObtenerResumenReserva(idReserva);

            if (resumen == null)
            {
                TempData["Error"] = "La reserva ha expirado o no es válida.";
                return RedirectToAction("Cartelera", "Evento");
            }

            var modelo = new PagoViewModel
            {
                IdReserva = resumen.IdReserva,
                Monto = resumen.Total,
                NombreEvento = resumen.NombreEvento,
                Cantidad = resumen.Cantidad,
                NombreSector = resumen.NombreSector
            };

            ViewBag.SegundosRestantes = await reservaService.ObtenerSegundosRestantes(idReserva);

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int idReserva)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (await EsReservaDelUsuario(idReserva, idUsuario))
            {
                await reservaService.CancelarReserva(idReserva);

                using SqlConnection conexion = conexionBD.ObtenerConexion();
                await conexion.OpenAsync();
                string query = @"
                    SELECT S.IdEvento 
                    FROM Reservas R
                    INNER JOIN Sectores S ON R.IdSector = S.IdSector
                    WHERE R.IdReserva = @IdReserva";

                using SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;

                object? result = await cmd.ExecuteScalarAsync();
                if (result != null)
                {
                    int idEvento = Convert.ToInt32(result);
                    return RedirectToAction("Detalle", "Evento", new { id = idEvento });
                }
            }

            return RedirectToAction("Cartelera", "Evento");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagar(PagoViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.SegundosRestantes = await reservaService.ObtenerSegundosRestantes(modelo.IdReserva);
                return View(modelo);
            }

            if (!ValidarFechaVencimiento(modelo.FechaVencimiento))
            {
                ModelState.AddModelError("FechaVencimiento", "La tarjeta está vencida.");
                ViewBag.SegundosRestantes = await reservaService.ObtenerSegundosRestantes(modelo.IdReserva);
                return View(modelo);
            }

            string tarjetaLimpia = modelo.NumeroTarjeta.Replace(" ", "");
            var resultado = await pagoService.ProcesarPago(modelo.IdReserva, modelo.Monto, tarjetaLimpia);

            TempData["EstadoPago"] = resultado.Estado;
            TempData["MensajePago"] = resultado.Mensaje;
            TempData["IdReserva"] = modelo.IdReserva;

            return RedirectToAction("ResultadoPago");
        }

        [HttpGet]
        public IActionResult ResultadoPago()
        {
            if (TempData["EstadoPago"] == null) return RedirectToAction("Cartelera", "Evento");
            ViewBag.Estado = TempData["EstadoPago"].ToString();
            ViewBag.Mensaje = TempData["MensajePago"].ToString();
            ViewBag.IdReserva = TempData["IdReserva"];
            return View();
        }

        private bool ValidarFechaVencimiento(string fecha)
        {
            if (!DateTime.TryParseExact(fecha, "MM/yy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime fechaVenc))
                return false;
            var ultimoDiaMes = new DateTime(fechaVenc.Year, fechaVenc.Month, DateTime.DaysInMonth(fechaVenc.Year, fechaVenc.Month));
            return ultimoDiaMes >= DateTime.Today;
        }

        private async Task<bool> EsReservaDelUsuario(int idReserva, int idUsuario)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            string query = "SELECT COUNT(1) FROM Reservas WHERE IdReserva = @IdReserva AND IdUsuario = @IdUsuario";
            using SqlCommand cmd = new SqlCommand(query, conexion);
            cmd.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
            cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
    }
}