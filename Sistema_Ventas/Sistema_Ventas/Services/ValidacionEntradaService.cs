using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class ValidacionEntradaService
    {
        private readonly ConexionBD conexionBD;

        public ValidacionEntradaService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<bool> Validar(string codigo, int idOrganizador)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            // Actualiza la entrada en una sola operación para evitar que se valide dos veces.
            string consulta = @"
                UPDATE E
                SET E.Estado = 'Usada', E.FechaUso = GETDATE()
                FROM Entradas E
                INNER JOIN Compras C ON E.IdCompra = C.IdCompra
                INNER JOIN Reservas R ON C.IdReserva = R.IdReserva
                INNER JOIN Sectores S ON R.IdSector = S.IdSector
                INNER JOIN Eventos V ON S.IdEvento = V.IdEvento
                WHERE E.Codigo = @Codigo
                  AND E.Estado = 'Emitida'
                  AND C.Estado = 'Pagada'
                  AND R.Estado = 'Pagada'
                  AND V.Activo = 1
                  AND V.IdOrganizador = @IdOrganizador";
            // Los filtros son para garantizar que la entrada pertenezca a un evento activo del organizador
            // y que las compra como la reserva esten pagadas.

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@Codigo", SqlDbType.VarChar, 50).Value = codigo.Trim().ToUpperInvariant();
            comando.Parameters.Add("@IdOrganizador", SqlDbType.Int).Value = idOrganizador;

            // Una fila actualizada indica que la entrada cumplió todas las condiciones.
            return await comando.ExecuteNonQueryAsync() == 1;
        }
    }
}
