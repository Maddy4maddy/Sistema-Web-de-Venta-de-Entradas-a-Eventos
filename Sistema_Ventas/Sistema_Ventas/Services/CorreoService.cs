
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Sistema_Ventas.Services
{
    public class CorreoService
    {
        private readonly IConfiguration configuration;

        public CorreoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task EnviarCodigoRecuperacion(string correoDestino, string codigo)
        {
            string correoEmisor = configuration["Correo:Gmail"]
                ?? throw new Exception("No se configuró el correo de Gmail.");

            string contrasenaAplicacion = configuration["Correo:ContrasenaAplicacion"]
                ?? throw new Exception("No se configuró la contraseña de aplicación.");

            var mensaje = new MimeMessage();

            mensaje.From.Add(new MailboxAddress("EVENTIA", correoEmisor));
            mensaje.To.Add(MailboxAddress.Parse(correoDestino));

            mensaje.Subject = "Recuperación de contraseña - EVENTIA";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 500px; margin: auto; padding: 25px; border: 1px solid #ddd; border-radius: 10px;'>
                        <h2 style='color: #24344D;'>EVENTIA</h2>
                        <h3>Recuperación de contraseña</h3>
                        <p>Hemos recibido una solicitud para recuperar la contraseña de tu cuenta.</p>
                        <p>Tu código de recuperación es:</p>
                        <h1 style='color: #426A91; letter-spacing: 5px;'>{codigo}</h1>
                        <p>Este código tiene una validez de 10 minutos.</p>
                        <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                    </div>"
            };

            using var cliente = new SmtpClient();

            await cliente.ConnectAsync(
                "smtp.gmail.com",
                587,
                SecureSocketOptions.StartTls
            );

            await cliente.AuthenticateAsync(
                correoEmisor,
                contrasenaAplicacion
            );

            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);
        }
    }
}
