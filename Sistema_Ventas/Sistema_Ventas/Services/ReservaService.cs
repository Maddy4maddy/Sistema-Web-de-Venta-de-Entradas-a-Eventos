using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class ReservaService
    {
        private readonly ConexionBD conexionBD;

        public ReservaService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<int?> ObtenerReservaPendiente(int idUsuario)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            string query = @"
                SELECT TOP 1 IdReserva 
                FROM Reservas 
                WHERE IdUsuario = @IdUsuario 
                  AND Estado = 'Pendiente' AND FechaExpiracion > GETDATE()
                ORDER BY FechaReserva DESC";

            using SqlCommand cmd = new SqlCommand(query, conexion);
            cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
            object? result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : null;
        }

        public async Task<int> CrearReserva(int idUsuario, int idSector, int cantidad)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string queryReserva = @"
                SELECT TOP 1 IdReserva 
                FROM Reservas 
                WHERE IdUsuario = @IdUsuario AND IdSector = @IdSector 
                  AND Estado = 'Pendiente' AND FechaExpiracion > GETDATE()";

            using (SqlCommand cmdCheck = new SqlCommand(queryReserva, conexion))
            {
                cmdCheck.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                cmdCheck.Parameters.Add("@IdSector", SqlDbType.Int).Value = idSector;
                object? existente = await cmdCheck.ExecuteScalarAsync();

                if (existente != null)
                {
                    return Convert.ToInt32(existente);
                }
            }

            // Verificar disponibilidad real
            string consultaDisponibilidad = @"
                SELECT Capacidad - ISNULL((
                    SELECT SUM(Cantidad) FROM Reservas 
                    WHERE IdSector = @IdSector AND Estado = 'Pendiente' AND FechaExpiracion > GETDATE()
                ), 0) AS Disponibles
                FROM Sectores WHERE IdSector = @IdSector";

            using SqlCommand cmdDisp = new SqlCommand(consultaDisponibilidad, conexion);
            cmdDisp.Parameters.Add("@IdSector", SqlDbType.Int).Value = idSector;

            object? result = await cmdDisp.ExecuteScalarAsync();
            int disponibles = result != null ? Convert.ToInt32(result) : 0;

            if (disponibles < cantidad)
                throw new Exception("No hay suficientes entradas disponibles en este sector.");

            // Insertar nueva reserva
            string consulta = @"
                INSERT INTO Reservas (IdUsuario, IdSector, Cantidad, FechaExpiracion, Estado)
                OUTPUT INSERTED.IdReserva
                VALUES (@IdUsuario, @IdSector, @Cantidad, DATEADD(MINUTE, 10, GETDATE()), 'Pendiente')";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
            comando.Parameters.Add("@IdSector", SqlDbType.Int).Value = idSector;
            comando.Parameters.Add("@Cantidad", SqlDbType.Int).Value = cantidad;

            return (int)(await comando.ExecuteScalarAsync())!;
        }

        // CANCELAR RESERVA 
        public async Task CancelarReserva(int idReserva)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            string consulta = "UPDATE Reservas SET Estado = 'Rechazado' WHERE IdReserva = @IdReserva";
            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
            await comando.ExecuteNonQueryAsync();
        }

        // LIBERAR RESERVAS EXPIRADAS
        public async Task LiberarReservasExpiradas(int idUsuario)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            string consulta = @"
                UPDATE Reservas 
                SET Estado = 'Rechazado' 
                WHERE IdUsuario = @IdUsuario AND Estado = 'Pendiente' AND FechaExpiracion <= GETDATE()";
            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
            await comando.ExecuteNonQueryAsync();
        }

        // OBTENER TIEMPO RESTANTE EN SEGUNDOS
        public async Task<int> ObtenerSegundosRestantes(int idReserva)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            string consulta = "SELECT DATEDIFF(SECOND, GETDATE(), FechaExpiracion) FROM Reservas WHERE IdReserva = @IdReserva AND Estado = 'Pendiente'";
            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
            object? result = await comando.ExecuteScalarAsync();
            return result != null ? Math.Max(0, Convert.ToInt32(result)) : 0;
        }
    }
}