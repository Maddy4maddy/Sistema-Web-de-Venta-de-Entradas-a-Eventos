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
                    C.CodigoEntrada,
                    C.FechaCompra
                FROM Compras C
                INNER JOIN Reservas R ON C.IdReserva = R.IdReserva
                INNER JOIN Sectores S ON R.IdSector = S.IdSector
                INNER JOIN Eventos E ON S.IdEvento = E.IdEvento
                WHERE C.IdUsuario = @IdUsuario
                ORDER BY C.FechaCompra DESC";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;

            using SqlDataReader lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                compras.Add(new CompraHistorialViewModel
                {
                    IdCompra = Convert.ToInt32(lector["IdCompra"]),
                    NombreEvento = lector["NombreEvento"].ToString()!,
                    FechaEvento = Convert.ToDateTime(lector["FechaEvento"]),
                    NombreSector = lector["NombreSector"].ToString()!,
                    Cantidad = Convert.ToInt32(lector["Cantidad"]),
                    Total = Convert.ToDecimal(lector["Total"]),
                    CodigoEntrada = lector["CodigoEntrada"].ToString()!,
                    FechaCompra = Convert.ToDateTime(lector["FechaCompra"])
                });
            }

            return compras;
        }
    }
}