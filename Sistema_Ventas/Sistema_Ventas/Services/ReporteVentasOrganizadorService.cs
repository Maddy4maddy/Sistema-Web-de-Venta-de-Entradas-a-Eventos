using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using Sistema_Ventas.Models;
using Sistema_Ventas.ViewModels;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class ReporteVentasOrganizadorService
    {
        private readonly ConexionBD conexionBD;

        public ReporteVentasOrganizadorService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<ReporteVentasOrganizadorViewModel?>
            ObtenerReporteVentas(int idOrganizador, int? idEvento)
        {
            var modelo = new ReporteVentasOrganizadorViewModel();

            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consultaEventos = @"
                SELECT IdEvento, Nombre, Fecha
                FROM Eventos
                WHERE IdOrganizador = @IdOrganizador
                ORDER BY Fecha DESC";

            using (SqlCommand comandoEventos =
                new SqlCommand(consultaEventos, conexion))
            {
                comandoEventos.Parameters.Add(
                    "@IdOrganizador", SqlDbType.Int).Value = idOrganizador;

                using SqlDataReader lector =
                    await comandoEventos.ExecuteReaderAsync();

                while (await lector.ReadAsync())
                {
                    modelo.Eventos.Add(new Evento
                    {
                        IdEvento = Convert.ToInt32(lector["IdEvento"]),
                        Nombre = lector["Nombre"].ToString()!,
                        Fecha = Convert.ToDateTime(lector["Fecha"])
                    });
                }
            }

            if (!idEvento.HasValue)
            {
                return modelo;
            }

            Evento? eventoSeleccionado = modelo.Eventos
                .FirstOrDefault(evento => evento.IdEvento == idEvento.Value);

            if (eventoSeleccionado == null)
            {
                return null;
            }

            modelo.IdEventoSeleccionado = eventoSeleccionado.IdEvento;
            modelo.NombreEvento = eventoSeleccionado.Nombre;
            modelo.FechaEvento = eventoSeleccionado.Fecha;

            string consultaResumen = @"
                SELECT
                    (
                        SELECT ISNULL(SUM(R.Cantidad), 0)
                        FROM Reservas R
                        INNER JOIN Sectores S ON S.IdSector = R.IdSector
                        WHERE S.IdEvento = @IdEvento
                          AND R.Estado = 'Pagada'
                          AND EXISTS (
                              SELECT 1
                              FROM Compras C
                              WHERE C.IdReserva = R.IdReserva
                                AND C.Estado = 'Pagada'
                          )
                    ) AS EntradasVendidas,
                    (
                        SELECT ISNULL(SUM(C.Total), 0)
                        FROM Compras C
                        INNER JOIN Reservas R ON R.IdReserva = C.IdReserva
                        INNER JOIN Sectores S ON S.IdSector = R.IdSector
                        WHERE S.IdEvento = @IdEvento
                          AND R.Estado = 'Pagada'
                          AND C.Estado = 'Pagada'
                    ) AS RecaudacionTotal";

            using (SqlCommand comandoResumen =
                new SqlCommand(consultaResumen, conexion))
            {
                comandoResumen.Parameters.Add(
                    "@IdEvento", SqlDbType.Int).Value = idEvento.Value;

                using SqlDataReader lector =
                    await comandoResumen.ExecuteReaderAsync();

                if (await lector.ReadAsync())
                {
                    modelo.EntradasVendidas =
                        Convert.ToInt32(lector["EntradasVendidas"]);

                    modelo.RecaudacionTotal =
                        Convert.ToDecimal(lector["RecaudacionTotal"]);
                }
            }

            string consultaSectores = @"
                SELECT
                    S.Nombre,
                    S.Capacidad,
                    (
                        SELECT ISNULL(SUM(R.Cantidad), 0)
                        FROM Reservas R
                        WHERE R.IdSector = S.IdSector
                          AND R.Estado = 'Pagada'
                          AND EXISTS (
                              SELECT 1
                              FROM Compras C
                              WHERE C.IdReserva = R.IdReserva
                                AND C.Estado = 'Pagada'
                          )
                    ) AS EntradasVendidas
                FROM Sectores S
                WHERE S.IdEvento = @IdEvento
                ORDER BY S.Nombre";

            using (SqlCommand comandoSectores =
                new SqlCommand(consultaSectores, conexion))
            {
                comandoSectores.Parameters.Add(
                    "@IdEvento", SqlDbType.Int).Value = idEvento.Value;

                using SqlDataReader lector =
                    await comandoSectores.ExecuteReaderAsync();

                while (await lector.ReadAsync())
                {
                    modelo.Sectores.Add(new ReporteSectorViewModel
                    {
                        NombreSector = lector["Nombre"].ToString()!,
                        Capacidad = Convert.ToInt32(lector["Capacidad"]),
                        EntradasVendidas =
                            Convert.ToInt32(lector["EntradasVendidas"])
                    });
                }
            }

            return modelo;
        }
    }
}
