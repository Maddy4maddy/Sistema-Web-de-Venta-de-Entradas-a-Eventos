
using Sistema_Ventas.Data;
using Microsoft.Data.SqlClient;

namespace Sistema_Ventas.Services
{
    public class conexionService
    {
        private readonly ConexionBD conexionBD;

        public conexionService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<string> ProbarConexion()
        {
            try
            {
                using SqlConnection conexion = conexionBD.ObtenerConexion();

                await conexion.OpenAsync();

                return "¡Conexión exitosa con SQL Server y SistemaVentas!";
            }
            catch (Exception ex)
            {
                return "Error de conexión: " + ex.Message;
            }
        }
    }
}
