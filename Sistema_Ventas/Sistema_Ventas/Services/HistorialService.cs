using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using Sistema_Ventas.ViewModels;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class HistorialService
    {
        private readonly ConexionBD conexionBD;

        public HistorialService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<List<CompraHistorialViewModel>> ObtenerComprasPorUsuario(int idUsuario)
        {
            var compras = new List<CompraHistorialViewModel>();

            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                SELECT
                    C.IdCompra,
                    E.Nombre AS NombreEvento,
                    E.Fecha AS FechaEvento,
                    S.Nombre AS NombreSector,
                    R.Cantidad,
                    C.Total,
                    T.Codigo,
                    T.Estado AS EstadoEntrada,
                    C.FechaCompra,
                    C.Estado AS EstadoCompra,
                    R.Estado AS EstadoReserva,
                    CASE WHEN E.Fecha > GETDATE() THEN 1 ELSE 0 END AS EventoFuturo
                FROM Compras C
                INNER JOIN Reservas R ON C.IdReserva = R.IdReserva
                INNER JOIN Sectores S ON R.IdSector = S.IdSector
                INNER JOIN Eventos E ON S.IdEvento = E.IdEvento
                LEFT JOIN Entradas T ON T.IdCompra = C.IdCompra
                WHERE C.IdUsuario = @IdUsuario
                ORDER BY C.FechaCompra DESC, C.IdCompra DESC, T.IdEntrada";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;

            using SqlDataReader lector = await comando.ExecuteReaderAsync();
            CompraHistorialViewModel? compraActual = null;

            while (await lector.ReadAsync())
            {
                int idCompra = Convert.ToInt32(lector["IdCompra"]);
                if (compraActual == null || compraActual.IdCompra != idCompra)
                {
                    compraActual = new CompraHistorialViewModel
                    {
                        IdCompra = idCompra,
                        NombreEvento = lector["NombreEvento"].ToString()!,
                        FechaEvento = Convert.ToDateTime(lector["FechaEvento"]),
                        NombreSector = lector["NombreSector"].ToString()!,
                        Cantidad = Convert.ToInt32(lector["Cantidad"]),
                        Total = Convert.ToDecimal(lector["Total"]),
                        FechaCompra = Convert.ToDateTime(lector["FechaCompra"]),
                        EstadoCompra = lector["EstadoCompra"].ToString()!,
                        EventoFuturo = Convert.ToInt32(lector["EventoFuturo"]) == 1 &&
                            lector["EstadoReserva"].ToString() == "Pagada"
                    };
                    compras.Add(compraActual);
                }

                if (lector["Codigo"] is string codigo)
                {
                    compraActual.CodigosEntradas.Add(codigo);
                    if (lector["EstadoEntrada"].ToString() == "Emitida")
                    {
                        compraActual.EntradasEmitidas++;
                    }
                }
            }

            return compras;
        }
    }
}
