
using Microsoft.Data.SqlClient;

namespace Sistema_Ventas.Data
{
    public class ConexionBD
    {
        private readonly string cadenaConexion;

        public ConexionBD(IConfiguration configuration)
        {
            cadenaConexion = configuration.GetConnectionString("ConexionSQL")
                ?? throw new Exception("No se encontró la cadena de conexión.");
        }

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(cadenaConexion);
        }
    }
}
