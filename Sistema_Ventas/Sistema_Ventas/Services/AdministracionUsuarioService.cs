using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using Sistema_Ventas.Models;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class AdministracionUsuarioService
    {
        private readonly ConexionBD conexionBD;

        public AdministracionUsuarioService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<List<Usuario>> ObtenerUsuarios()
        {
            var usuarios = new List<Usuario>();

            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                SELECT U.IdUsuario, U.Nombre, U.Apellido, U.Correo,
                       U.IdRol, U.FechaRegistro, U.Activo,
                       R.Nombre AS NombreRol
                FROM Usuarios U
                INNER JOIN Roles R ON U.IdRol = R.IdRol
                ORDER BY U.Apellido, U.Nombre";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            using SqlDataReader lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                usuarios.Add(new Usuario
                {
                    IdUsuario = Convert.ToInt32(lector["IdUsuario"]),
                    Nombre = lector["Nombre"].ToString()!,
                    Apellido = lector["Apellido"].ToString()!,
                    Correo = lector["Correo"].ToString()!,
                    IdRol = Convert.ToInt32(lector["IdRol"]),
                    FechaRegistro = Convert.ToDateTime(lector["FechaRegistro"]),
                    Activo = Convert.ToBoolean(lector["Activo"]),
                    NombreRol = lector["NombreRol"].ToString()!
                });
            }

            return usuarios;
        }

        public async Task<List<Rol>> ObtenerRoles()
        {
            var roles = new List<Rol>();

            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                SELECT IdRol, Nombre, Descripcion
                FROM Roles
                ORDER BY Nombre";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            using SqlDataReader lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                roles.Add(new Rol
                {
                    IdRol = Convert.ToInt32(lector["IdRol"]),
                    Nombre = lector["Nombre"].ToString()!,
                    Descripcion = lector["Descripcion"] == DBNull.Value
                        ? string.Empty
                        : lector["Descripcion"].ToString()!
                });
            }

            return roles;
        }

        public async Task<bool> CambiarEstado(int idUsuario, bool activo)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                UPDATE Usuarios
                SET Activo = @Activo
                WHERE IdUsuario = @IdUsuario";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = activo;
            comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;

            return await comando.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> CambiarRol(int idUsuario, int idRol)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                UPDATE Usuarios
                SET IdRol = @IdRol
                WHERE IdUsuario = @IdUsuario";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdRol", SqlDbType.Int).Value = idRol;
            comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;

            return await comando.ExecuteNonQueryAsync() == 1;
        }
    }
}
