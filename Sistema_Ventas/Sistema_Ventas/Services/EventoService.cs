
using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using Sistema_Ventas.Models;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class EventoService
    {
        private readonly ConexionBD conexionBD;

        public EventoService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<List<Evento>> ObtenerEventosActivos(
            string? nombre = null,
            string? categoria = null,
            DateTime? fecha = null,
            string? ciudad = null)
        {
            List<Evento> eventos = new List<Evento>();

            using SqlConnection conexion = conexionBD.ObtenerConexion();

            string consulta = @"
                SELECT
                    IdEvento,
                    Nombre,
                    Descripcion,
                    Categoria,
                    Fecha,
                    Lugar,
                    Ciudad,
                    Imagen,
                    PrecioInicial,
                    Activo,
                    FechaCreacion
                FROM Eventos
                WHERE Activo = 1
                    AND (@Nombre IS NULL OR Nombre LIKE '%' + @Nombre + '%')
                    AND (@Categoria IS NULL OR Categoria = @Categoria)
                    AND (@Fecha IS NULL OR
                        (Fecha >= @Fecha AND Fecha < DATEADD(DAY, 1, @Fecha)))
                    AND (@Ciudad IS NULL OR Ciudad LIKE '%' + @Ciudad + '%')
                ORDER BY Fecha ASC";

            using SqlCommand comando = new SqlCommand(consulta, conexion);

            comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 150).Value =
                string.IsNullOrWhiteSpace(nombre)
                    ? DBNull.Value
                    : nombre.Trim();

            comando.Parameters.Add("@Categoria", SqlDbType.VarChar, 100).Value =
                string.IsNullOrWhiteSpace(categoria)
                    ? DBNull.Value
                    : categoria.Trim();

            comando.Parameters.Add("@Fecha", SqlDbType.DateTime2).Value =
                fecha.HasValue
                    ? fecha.Value.Date
                    : DBNull.Value;

            comando.Parameters.Add("@Ciudad", SqlDbType.VarChar, 100).Value =
                string.IsNullOrWhiteSpace(ciudad)
                    ? DBNull.Value
                    : ciudad.Trim();

            await conexion.OpenAsync();

            using SqlDataReader reader = await comando.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Evento evento = new Evento
                {
                    IdEvento = Convert.ToInt32(reader["IdEvento"]),
                    Nombre = reader["Nombre"].ToString()!,
                    Descripcion = reader["Descripcion"] == DBNull.Value
                        ? string.Empty
                        : reader["Descripcion"].ToString()!,
                    Categoria = reader["Categoria"].ToString()!,
                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                    Lugar = reader["Lugar"].ToString()!,
                    Ciudad = reader["Ciudad"].ToString()!,
                    Imagen = reader["Imagen"] == DBNull.Value
                        ? string.Empty
                        : reader["Imagen"].ToString()!,
                    PrecioInicial = Convert.ToDecimal(reader["PrecioInicial"]),
                    Activo = Convert.ToBoolean(reader["Activo"]),
                    FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"])
                };

                eventos.Add(evento);
            }

            return eventos;
        }
    }
}
