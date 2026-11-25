using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class AnulacionService
    {
        private readonly ConexionBD conexionBD;

        public AnulacionService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<bool> Solicitar(int idCompra, int idUsuario, string motivo)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            using SqlTransaction transaccion = conexion.BeginTransaction(); // Todo debe completarse junto: si falla un paso, la compra no queda anulada a medias

            try
            {
                int idReserva = 0;
                int cantidad = 0;
                decimal total = 0;
                bool compraValida;

                // busca solo una compra del cliente, pagada y que el evento no haya iniciado
                // UPDLOCK y HOLDLOCK mantienen bloqueada esa compra durante la transacción
                // para que dos solicitudes simultáneas no aprueben dos reembolsos
                string consultaCompra = @"
                    SELECT C.IdReserva, R.Cantidad, C.Total
                    FROM Compras C WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN Reservas R ON R.IdReserva = C.IdReserva
                    INNER JOIN Sectores S ON S.IdSector = R.IdSector
                    INNER JOIN Eventos E ON E.IdEvento = S.IdEvento
                    WHERE C.IdCompra = @IdCompra AND C.IdUsuario = @IdUsuario
                      AND C.Estado = 'Pagada' AND R.Estado = 'Pagada'
                      AND E.Fecha > GETDATE()";

                using (SqlCommand comando = new SqlCommand(consultaCompra, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                    comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                    using SqlDataReader reader = await comando.ExecuteReaderAsync();
                    compraValida = await reader.ReadAsync();
                    if (compraValida)
                    {
                        idReserva = Convert.ToInt32(reader["IdReserva"]);
                        cantidad = Convert.ToInt32(reader["Cantidad"]);
                        total = Convert.ToDecimal(reader["Total"]);
                    }
                }

                if (!compraValida) // si la compra no existe o dejó de cumplir alguna condición, no se toca nada
                {
                    transaccion.Rollback();
                    return false;
                }


                // cuenta todas las entradas y, por separado, las que todavia están emitidas
                // Una entrada Usada o Anulada impide devolver la compra completa
                string consultaEntradas = @"
                    SELECT COUNT(*) AS TotalEntradas,
                           COUNT(CASE WHEN Estado = 'Emitida' THEN 1 END) AS Emitidas
                    FROM Entradas WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdCompra = @IdCompra";


                // tambien comprueba que no falten entradas respecto a la cantidad comprada.
                bool entradasValidas;
                using (SqlCommand comando = new SqlCommand(consultaEntradas, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                    using SqlDataReader reader = await comando.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    entradasValidas = cantidad > 0 && Convert.ToInt32(reader["TotalEntradas"]) == cantidad &&
                        Convert.ToInt32(reader["Emitidas"]) == cantidad;
                }

                if (!entradasValidas)
                {
                    transaccion.Rollback();
                    return false;
                }

                // Invalida los códigos sin borrar las entradas: permanecerán en el historial,
                // pero RF-14 ya no podrá validarlas como entradas Emitidas.
                string actualizarEntradas = @"
                    UPDATE Entradas SET Estado = 'Anulada'
                    WHERE IdCompra = @IdCompra AND Estado = 'Emitida'";
                using (SqlCommand comando = new SqlCommand(actualizarEntradas, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                    if (await comando.ExecuteNonQueryAsync() != cantidad)
                    {
                        transaccion.Rollback();
                        return false;
                    }
                }


                // Vuelve a comprobar la fecha al actualizar: pudo empezar el evento
                // mientras se procesaba la solicitud.
                string actualizarCompra = @"
                    UPDATE C SET Estado = 'Anulada'
                    FROM Compras C
                    INNER JOIN Reservas R ON R.IdReserva = C.IdReserva
                    INNER JOIN Sectores S ON S.IdSector = R.IdSector
                    INNER JOIN Eventos E ON E.IdEvento = S.IdEvento
                    WHERE C.IdCompra = @IdCompra AND C.IdUsuario = @IdUsuario
                      AND C.Estado = 'Pagada' AND E.Fecha > GETDATE()";
                using (SqlCommand comando = new SqlCommand(actualizarCompra, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                    comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                    if (await comando.ExecuteNonQueryAsync() != 1)
                    {
                        transaccion.Rollback();
                        return false;
                    }
                }
                // Una reserva Pagada ocupa cupo. Al pasarla a Anulada deja de descontarse
                // de la disponibilidad del sector.
                string actualizarReserva = @"
                    UPDATE Reservas SET Estado = 'Anulada'
                    WHERE IdReserva = @IdReserva AND Estado = 'Pagada'";
                using (SqlCommand comando = new SqlCommand(actualizarReserva, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
                    if (await comando.ExecuteNonQueryAsync() != 1)
                    {
                        transaccion.Rollback();
                        return false;
                    }
                }

                // guarda quién pidió la anulación (a través de la compra), su motivo y
                // cuánto se habría devuelto. No se envía dinero a ninguna tarjeta
                string registrarSolicitud = @"
                    INSERT INTO SolicitudesAnulacion
                        (IdCompra, Motivo, Estado, FechaSolicitud, FechaResolucion, MontoReembolso)
                    VALUES (@IdCompra, @Motivo, 'Aprobada', GETDATE(), GETDATE(), @MontoReembolso)";
                using (SqlCommand comando = new SqlCommand(registrarSolicitud, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                    comando.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;
                    SqlParameter monto = comando.Parameters.Add("@MontoReembolso", SqlDbType.Decimal);
                    monto.Precision = 10;
                    monto.Scale = 2;
                    monto.Value = total;
                    await comando.ExecuteNonQueryAsync();
                }

                //quedan guardadso los campos
                transaccion.Commit();
                return true;
            }
            catch
            {
                //si algo falla no deja que se anule la compra
                transaccion.Rollback();
                throw;
            }
        }
    }
}
