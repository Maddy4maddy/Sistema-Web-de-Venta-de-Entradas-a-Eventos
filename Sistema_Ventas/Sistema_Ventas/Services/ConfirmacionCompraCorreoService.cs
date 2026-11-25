using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using MimeKit.Utils;
using QRCoder;
using Sistema_Ventas.Data;
using System.Data;
using System.Net;
using System.Text;

namespace Sistema_Ventas.Services
{
    //parte de RF-10 Y 9 
    public class ConfirmacionCompraCorreoService
    {
        private readonly ConexionBD conexionBD;
        private readonly IConfiguration configuration;

        public ConfirmacionCompraCorreoService(ConexionBD conexionBD, IConfiguration configuration)
        {
            this.conexionBD = conexionBD;
            this.configuration = configuration;
        }

        public async Task EnviarConfirmacion(int idCompra, int idUsuario)
        {
            string correoDestino;
            string nombreEvento;
            string nombreSector;
            string lugar;
            DateTime fechaEvento;
            int cantidad;
            decimal total;
            List<string> codigos = new List<string>();

            //Obtener  datos de compra y sus entradas
            using (SqlConnection conexion = conexionBD.ObtenerConexion())
            {
                await conexion.OpenAsync();

                string consulta = @"
                    SELECT U.Correo, E.Nombre, E.Lugar, E.Fecha, S.Nombre AS Sector,
                           R.Cantidad, C.Total, T.Codigo
                    FROM Compras C
                    INNER JOIN Usuarios U ON C.IdUsuario = U.IdUsuario
                    INNER JOIN Reservas R ON C.IdReserva = R.IdReserva
                    INNER JOIN Sectores S ON R.IdSector = S.IdSector
                    INNER JOIN Eventos E ON S.IdEvento = E.IdEvento
                    INNER JOIN Entradas T ON T.IdCompra = C.IdCompra
                    WHERE C.IdCompra = @IdCompra AND C.IdUsuario = @IdUsuario
                      AND R.Estado = 'Pagada' AND T.Estado = 'Emitida'
                    ORDER BY T.IdEntrada";

                using SqlCommand comando = new SqlCommand(consulta, conexion);
                comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;

                using SqlDataReader reader = await comando.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException("La compra no tiene entradas emitidas.");
                }

                // Leer los datos generales de la compra
                correoDestino = reader.GetString(0);
                nombreEvento = reader.GetString(1);
                lugar = reader.GetString(2);
                fechaEvento = reader.GetDateTime(3);
                nombreSector = reader.GetString(4);
                cantidad = reader.GetInt32(5);
                total = reader.GetDecimal(6);
                codigos.Add(reader.GetString(7));

                // Obtener los demás códigos de entrada
                while (await reader.ReadAsync())
                {
                    codigos.Add(reader.GetString(7));
                }
            }

            // si hay datos incorrectos evita enviar el correo
            if (cantidad <= 0 || codigos.Count != cantidad)
            {
                throw new InvalidOperationException("La cantidad de entradas emitidas no coincide con la compra.");
            }

            //// Obtener la cuenta emisora
            string correoEmisor = configuration["Correo:Gmail"]
                ?? throw new InvalidOperationException("No está configurado el remitente del correo.");
            string contrasenaAplicacion = configuration["Correo:ContrasenaAplicacion"]
                ?? throw new InvalidOperationException("No está configurada la contraseña de aplicación.");


            // Esto es para preparar el mensaje, quien es emisor, receptor y asunto 
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("EVENTIA", correoEmisor));
            mensaje.To.Add(MailboxAddress.Parse(correoDestino));
            mensaje.Subject = "Confirmación de compra - EVENTIA";

            var cuerpo = new BodyBuilder();
            var texto = new StringBuilder();
            var html = new StringBuilder();

            // esto es para crear la versión de texto del comprobante
            texto.AppendLine("EVENTIA - Confirmación de compra");
            texto.AppendLine($"Evento: {nombreEvento}");
            texto.AppendLine($"Fecha: {fechaEvento:dd/MM/yyyy HH:mm}");
            texto.AppendLine($"Lugar: {lugar}");
            texto.AppendLine($"Sector: {nombreSector}");
            texto.AppendLine($"Cantidad: {cantidad}");
            texto.AppendLine($"Total pagado: ₡{total:N2}");
            texto.AppendLine();

            //y esto es para crear la version HTML del comprobante
            html.Append("<div style='font-family:Arial,sans-serif;max-width:560px;margin:auto;padding:24px'>");
            html.Append("<h2 style='color:#24344D'>EVENTIA</h2><h3>Confirmación de compra</h3>");
            html.Append($"<p><strong>Evento:</strong> {WebUtility.HtmlEncode(nombreEvento)}<br>");
            html.Append($"<strong>Fecha:</strong> {fechaEvento:dd/MM/yyyy HH:mm}<br>");
            html.Append($"<strong>Lugar:</strong> {WebUtility.HtmlEncode(lugar)}<br>");
            html.Append($"<strong>Sector:</strong> {WebUtility.HtmlEncode(nombreSector)}<br>");
            html.Append($"<strong>Cantidad:</strong> {cantidad}<br>");
            html.Append($"<strong>Total pagado:</strong> ₡{total:N2}</p>");

            // Generar un QR por cada entrada
            for (int i = 0; i < codigos.Count; i++)
            {
                string codigo = codigos[i];
                byte[] png = PngByteQRCodeHelper.GetQRCode(codigo, QRCodeGenerator.ECCLevel.Q, 6);
                var imagen = cuerpo.LinkedResources.Add($"entrada-{i + 1}.png", png, new ContentType("image", "png"));
                imagen.ContentId = MimeUtils.GenerateMessageId();

                texto.AppendLine($"Entrada {i + 1}: {codigo}");
                html.Append($"<div style='border-top:1px solid #ddd;padding:16px 0'><strong>Entrada {i + 1}</strong><br>");
                html.Append($"<span>{WebUtility.HtmlEncode(codigo)}</span><br>");
                html.Append($"<img src='cid:{imagen.ContentId}' alt='QR de la entrada {i + 1}' width='220' height='220'></div>");
            }

            html.Append("<p>Presenta el código o QR correspondiente al ingresar al evento.</p></div>");
            cuerpo.TextBody = texto.ToString();       // Completar el cuerpo del correo
            cuerpo.HtmlBody = html.ToString();
            mensaje.Body = cuerpo.ToMessageBody();


            // Enviar el comprobante al correo
            using var cliente = new SmtpClient { Timeout = 10000 };
            await cliente.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls); 
            await cliente.AuthenticateAsync(correoEmisor, contrasenaAplicacion);
            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);
        }
    }
}
