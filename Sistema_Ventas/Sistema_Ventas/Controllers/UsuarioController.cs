
using Microsoft.AspNetCore.Mvc;
using Sistema_Ventas.Models;
using Sistema_Ventas.Services;
using Sistema_Ventas.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Sistema_Ventas.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly UsuarioService usuarioService;
        private readonly CorreoService correoService;

        public UsuarioController(
            UsuarioService usuarioService,
            CorreoService correoService)
        {
            this.usuarioService = usuarioService;
            this.correoService = correoService;
        }

        // Mostrar formulario de registro
        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        // Procesar registro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            Usuario usuario = new Usuario
            {
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido,
                Correo = modelo.Correo,
                Contrasena = modelo.Contrasena
            };

            string resultado = await usuarioService.RegistrarUsuario(usuario);

            if (resultado == "Usuario registrado correctamente.")
            {
                TempData["Mensaje"] = resultado;
                return RedirectToAction("IniciarSesion");
            }

            ModelState.AddModelError(string.Empty, resultado);
            return View(modelo);
        }

        // Mostrar formulario de inicio de sesión
        [HttpGet]
        public IActionResult IniciarSesion()
        {
            return View();
        }

        // Procesar inicio de sesión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            Usuario? usuario = await usuarioService.IniciarSesion(
                modelo.Correo,
                modelo.Contrasena
            );

            if (usuario == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El correo o la contraseña son incorrectos."
                );

                return View(modelo);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.IdUsuario.ToString()
                ),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.NombreRol)
            };

            var identidad = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identidad);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction("Index", "Home");
        }

        // Cerrar sesión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("IniciarSesion");
        }

        // Mostrar acceso denegado
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return Content(
                "No tienes permisos para acceder a esta página."
            );
        }

        // Mostrar formulario para recuperar contraseña
        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            return View();
        }

        // Procesar solicitud de recuperación y enviar correo real
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarContrasena(
            SolicitarRecuperacionViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var resultado = await usuarioService.GenerarCodigoRecuperacion(
                modelo.Correo
            );

            if (!resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(modelo);
            }

            try
            {
                await correoService.EnviarCodigoRecuperacion(
                    modelo.Correo,
                    resultado.Codigo!
                );

                var vistaModelo = new RestablecerContrasenaViewModel
                {
                    Correo = modelo.Correo
                };

                TempData["MensajeCorreo"] =
                    "Se ha enviado un código de recuperación a tu correo electrónico.";

                return View("RestablecerContrasena", vistaModelo);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible enviar el correo. Verifica la configuración de Gmail e inténtalo nuevamente."
                );

                return View(modelo);
            }
        }

        // Mostrar formulario para introducir el código y nueva contraseña
        [HttpGet]
        public IActionResult RestablecerContrasena()
        {
            return View();
        }

        // Procesar el cambio de contraseña
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(
            RestablecerContrasenaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string resultado = await usuarioService.RestablecerContrasena(
                modelo.Correo,
                modelo.Codigo,
                modelo.NuevaContrasena
            );

            if (resultado == "La contraseña se actualizó correctamente.")
            {
                TempData["Mensaje"] = resultado;
                return RedirectToAction("IniciarSesion");
            }

            ModelState.AddModelError(string.Empty, resultado);
            return View(modelo);
        }
    }
}
