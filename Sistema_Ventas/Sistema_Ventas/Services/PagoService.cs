using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class PagoService
    {
        private readonly ConexionBD conexionBD;

        public PagoService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<(string Estado, string Mensaje)> ProcesarPago(int idReserva, decimal monto, string numeroTarjeta)
        {
            using (SqlConnection conexionCheck = conexionBD.ObtenerConexion())
            {
                await conexionCheck.OpenAsync();
                string queryCheck = "SELECT COUNT(1) FROM Reservas WHERE IdReserva = @IdReserva AND Estado = 'Pendiente' AND FechaExpiracion > GETDATE()";
                using SqlCommand cmdCheck = new SqlCommand(queryCheck, conexionCheck);
                cmdCheck.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
                int existe = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());

                if (existe == 0)
                    return ("Error", "Tu reserva ha expirado. Vuelve a seleccionar tus entradas.");
            }

            
            await Task.Delay(2000);

            // Obtener últimos 4 dígitos
            string ultimos4 = numeroTarjeta.Substring(numeroTarjeta.Length - 4);

            // Determinar resultado
            string estado;
            string mensaje;

            switch (numeroTarjeta)
            {
                case "4111111111111111":
                    estado = "Aprobado";
                    mensaje = "Pago aprobado. Tus entradas están listas.";
                    break;
                case "4000000000000002":
                    estado = "Rechazado";
                    mensaje = "Pago rechazado por fondos insuficientes.";
                    break;
                case "4000000000000003":
                    estado = "Rechazado";
                    mensaje = "Tarjeta bloqueada. Usa otro medio de pago.";
                    break;
                case "4000000000000009":
                    estado = "Error";
                    mensaje = "No pudimos procesar el pago. Intenta nuevamente.";
                    break;
                default:
                    estado = "Error";
                    mensaje = "Tarjeta no reconocida por la pasarela simulada.";
                    break;
            }

            // Registrar transacción
            await RegistrarTransaccion(idReserva, monto, ultimos4, estado);

            // Si es aprobado, marcar reserva como Pagada
            if (estado == "Aprobado")
            {
                await MarcarReservaComoPagada(idReserva);
            }

            return (estado, mensaje);
        }

        private async Task RegistrarTransaccion(int idReserva, decimal monto, string ultimos4, string estado)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string codigoTxn = $"TXN-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

            string consulta = @"
                INSERT INTO TransaccionesPago (IdReserva, CodigoTransaccion, Monto, Ultimos4Digitos, Estado)
                VALUES (@IdReserva, @CodigoTxn, @Monto, @Ultimos4, @Estado)";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
            comando.Parameters.Add("@CodigoTxn", SqlDbType.VarChar, 50).Value = codigoTxn;
            comando.Parameters.Add("@Monto", SqlDbType.Decimal).Value = monto;
            comando.Parameters.Add("@Ultimos4", SqlDbType.VarChar, 4).Value = ultimos4;
            comando.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = estado;

            await comando.ExecuteNonQueryAsync();
        }

        private async Task MarcarReservaComoPagada(int idReserva)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            string consulta = "UPDATE Reservas SET Estado = 'Pagada' WHERE IdReserva = @IdReserva";
            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
            await comando.ExecuteNonQueryAsync();
        }

        public async Task<dynamic?> ObtenerResumenReserva(int idReserva)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                SELECT 
                    R.IdReserva, 
                    R.Cantidad, 
                    S.Precio, 
                    S.Nombre AS NombreSector, 
                    E.Nombre AS NombreEvento,
                    (R.Cantidad * S.Precio) AS Total
                FROM Reservas R
                INNER JOIN Sectores S ON R.IdSector = S.IdSector
                INNER JOIN Eventos E ON S.IdEvento = E.IdEvento
                WHERE R.IdReserva = @IdReserva AND R.Estado = 'Pendiente' AND R.FechaExpiracion > GETDATE()";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;

            using SqlDataReader reader = await comando.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new
            {
                IdReserva = Convert.ToInt32(reader["IdReserva"]),
                Cantidad = Convert.ToInt32(reader["Cantidad"]),
                Precio = Convert.ToDecimal(reader["Precio"]),
                NombreSector = reader["NombreSector"].ToString(),
                NombreEvento = reader["NombreEvento"].ToString(),
                Total = Convert.ToDecimal(reader["Total"])
            };
        }
    }
}