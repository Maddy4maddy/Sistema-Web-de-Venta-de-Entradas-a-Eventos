using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class EntradaService
    {
        private readonly ConexionBD conexionBD;

        public EntradaService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<int?> GenerarEntradas(int idReserva)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            using SqlTransaction transaccion = conexion.BeginTransaction();

            try
            {
                int idUsuario = 0;
                int cantidad = 0;
                decimal total = 0;
                bool reservaPagada;

                string consultaReserva = @"
                    SELECT R.IdUsuario, R.Cantidad, R.Cantidad * S.Precio AS Total
                    FROM Reservas R WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN Sectores S ON R.IdSector = S.IdSector
                    WHERE R.IdReserva = @IdReserva
                      AND R.Estado = 'Pagada'";

                using (SqlCommand comandoReserva = new SqlCommand(consultaReserva, conexion, transaccion))
                {
                    comandoReserva.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;

                    using SqlDataReader reader = await comandoReserva.ExecuteReaderAsync();
                    reservaPagada = await reader.ReadAsync();
                    if (reservaPagada)
                    {
                        idUsuario = Convert.ToInt32(reader["IdUsuario"]);
                        cantidad = Convert.ToInt32(reader["Cantidad"]);
                        total = Convert.ToDecimal(reader["Total"]);
                    }
                }

                if (!reservaPagada || cantidad <= 0)
                {
                    transaccion.Rollback();
                    return null;
                }

                string consultaCompraExistente = @"
                    SELECT TOP 1 IdCompra
                    FROM Compras
                    WHERE IdReserva = @IdReserva";

                using (SqlCommand comandoCompraExistente = new SqlCommand(consultaCompraExistente, conexion, transaccion))
                {
                    comandoCompraExistente.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
                    object? compraExistente = await comandoCompraExistente.ExecuteScalarAsync();
                    if (compraExistente != null)
                    {
                        transaccion.Commit();
                        return Convert.ToInt32(compraExistente);
                    }
                }

                List<string> codigos = new List<string>();
                for (int i = 0; i < cantidad; i++)
                {
                    codigos.Add($"ENT-{Guid.NewGuid().ToString("N").ToUpper()}");
                }

                string consultaCompra = @"
                    INSERT INTO Compras (IdUsuario, IdReserva, Total, CodigoEntrada, FechaCompra)
                    OUTPUT INSERTED.IdCompra
                    VALUES (@IdUsuario, @IdReserva, @Total, @CodigoEntrada, GETDATE())";

                int idCompra;
                using (SqlCommand comandoCompra = new SqlCommand(consultaCompra, conexion, transaccion))
                {
                    comandoCompra.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                    comandoCompra.Parameters.Add("@IdReserva", SqlDbType.Int).Value = idReserva;
                    SqlParameter parametroTotal = comandoCompra.Parameters.Add("@Total", SqlDbType.Decimal);
                    parametroTotal.Precision = 10;
                    parametroTotal.Scale = 2;
                    parametroTotal.Value = total;
                    comandoCompra.Parameters.Add("@CodigoEntrada", SqlDbType.VarChar, 50).Value = codigos[0];
                    idCompra = Convert.ToInt32(await comandoCompra.ExecuteScalarAsync());
                }

                string consultaEntrada = @"
                    INSERT INTO Entradas (IdCompra, Codigo)
                    VALUES (@IdCompra, @Codigo)";

                foreach (string codigo in codigos)
                {
                    using SqlCommand comandoEntrada = new SqlCommand(consultaEntrada, conexion, transaccion);
                    comandoEntrada.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;
                    comandoEntrada.Parameters.Add("@Codigo", SqlDbType.VarChar, 50).Value = codigo;
                    await comandoEntrada.ExecuteNonQueryAsync();
                }

                transaccion.Commit();
                return idCompra;
            }
            catch
            {
                transaccion.Rollback();
                throw;
            }
        }

        public async Task<List<string>> ObtenerCodigos(int idCompra)
        {
            List<string> codigos = new List<string>();
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                SELECT Codigo
                FROM Entradas
                WHERE IdCompra = @IdCompra
                ORDER BY IdEntrada";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdCompra", SqlDbType.Int).Value = idCompra;

            using SqlDataReader reader = await comando.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                codigos.Add(reader["Codigo"].ToString()!);
            }

            return codigos;
        }
    }
}
