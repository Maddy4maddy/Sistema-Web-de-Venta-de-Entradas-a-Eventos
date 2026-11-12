
using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using Sistema_Ventas.Models;
using System.Data;
using System.Security.Cryptography;

namespace Sistema_Ventas.Services
{
    public class UsuarioService
    {
        private readonly ConexionBD conexionBD;

        public UsuarioService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<string> RestablecerContrasena(
    string correo,
    string codigo,
    string nuevaContrasena)
        {
            try
            {
                using SqlConnection conexion = conexionBD.ObtenerConexion();
                await conexion.OpenAsync();

                using SqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    string consulta = @"
                SELECT TOP 1
                    R.IdRecuperacion,
                    R.CodigoHash
                FROM RecuperacionContrasena R
                INNER JOIN Usuarios U
                    ON R.IdUsuario = U.IdUsuario
                WHERE U.Correo = @Correo
                    AND U.Activo = 1
                    AND R.Usado = 0
                    AND R.FechaExpiracion > GETDATE()
                ORDER BY R.FechaCreacion DESC";

                    int idRecuperacion;
                    string codigoHash;

                    using (SqlCommand comando = new SqlCommand(consulta, conexion, transaccion))
                    {
                        comando.Parameters.Add("@Correo", SqlDbType.VarChar, 254).Value = correo;

                        using SqlDataReader lector = await comando.ExecuteReaderAsync();

                        if (!await lector.ReadAsync())
                        {
                            await lector.CloseAsync();
                            await transaccion.RollbackAsync();

                            return "El código no existe, ya fue utilizado o ha expirado.";
                        }

                        idRecuperacion = Convert.ToInt32(lector["IdRecuperacion"]);
                        codigoHash = lector["CodigoHash"].ToString()!;
                    }

                    if (!BCrypt.Net.BCrypt.Verify(codigo, codigoHash))
                    {
                        await transaccion.RollbackAsync();
                        return "El código de recuperación es incorrecto.";
                    }

                    string nuevaContrasenaHash =
                        BCrypt.Net.BCrypt.HashPassword(nuevaContrasena);

                    string actualizarUsuario = @"
                UPDATE Usuarios
                SET Contrasena = @Contrasena
                WHERE Correo = @Correo";

                    using (SqlCommand comandoUsuario =
                        new SqlCommand(actualizarUsuario, conexion, transaccion))
                    {
                        comandoUsuario.Parameters.Add("@Contrasena", SqlDbType.VarChar, 255)
                            .Value = nuevaContrasenaHash;

                        comandoUsuario.Parameters.Add("@Correo", SqlDbType.VarChar, 254)
                            .Value = correo;

                        await comandoUsuario.ExecuteNonQueryAsync();
                    }

                    string actualizarCodigo = @"
                UPDATE RecuperacionContrasena
                SET Usado = 1
                WHERE IdRecuperacion = @IdRecuperacion";

                    using (SqlCommand comandoCodigo =
                        new SqlCommand(actualizarCodigo, conexion, transaccion))
                    {
                        comandoCodigo.Parameters.Add("@IdRecuperacion", SqlDbType.Int)
                            .Value = idRecuperacion;

                        await comandoCodigo.ExecuteNonQueryAsync();
                    }

                    await transaccion.CommitAsync();

                    return "La contraseña se actualizó correctamente.";
                }
                catch
                {
                    await transaccion.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return "Error al restablecer la contraseña: " + ex.Message;
            }
        }
        public async Task<(bool Exito, string Mensaje, string? Codigo)> GenerarCodigoRecuperacion(string correo)
        {
            try
            {
                using SqlConnection conexion = conexionBD.ObtenerConexion();
                await conexion.OpenAsync();

                string consultaUsuario = @"
            SELECT IdUsuario
            FROM Usuarios
            WHERE Correo = @Correo AND Activo = 1";

                using SqlCommand comandoUsuario = new SqlCommand(consultaUsuario, conexion);
                comandoUsuario.Parameters.Add("@Correo", SqlDbType.VarChar, 254).Value = correo;

                object? resultado = await comandoUsuario.ExecuteScalarAsync();

                if (resultado == null)
                {
                    return (false, "No se encontró una cuenta activa con ese correo.", null);
                }

                int idUsuario = Convert.ToInt32(resultado);

                string codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                string codigoHash = BCrypt.Net.BCrypt.HashPassword(codigo);

                string consulta = @"
            UPDATE RecuperacionContrasena
            SET Usado = 1
            WHERE IdUsuario = @IdUsuario AND Usado = 0;

            INSERT INTO RecuperacionContrasena
            (IdUsuario, CodigoHash, FechaExpiracion)
            VALUES
            (@IdUsuario, @CodigoHash, DATEADD(MINUTE, 10, GETDATE()));";

                using SqlCommand comando = new SqlCommand(consulta, conexion);

                comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                comando.Parameters.Add("@CodigoHash", SqlDbType.VarChar, 255).Value = codigoHash;

                await comando.ExecuteNonQueryAsync();

                return (true, "Código de recuperación generado correctamente.", codigo);
            }
            catch (Exception ex)
            {
                return (false, "Error al generar el código: " + ex.Message, null);
            }
        }

        public async Task<string> RegistrarUsuario(Usuario usuario)
        {
            try
            {
                using SqlConnection conexion = conexionBD.ObtenerConexion();

                await conexion.OpenAsync();

                string consultaRol = "SELECT IdRol FROM Roles WHERE Nombre = @Nombre";

                using SqlCommand comandoRol = new SqlCommand(consultaRol, conexion);

                comandoRol.Parameters.Add("@Nombre", SqlDbType.VarChar, 50).Value = "Cliente";

                object? resultadoRol = await comandoRol.ExecuteScalarAsync();

                if (resultadoRol == null)
                {
                    return "No se encontró el rol Cliente.";
                }

                int idRol = Convert.ToInt32(resultadoRol);

                string contrasenaHash = BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena);

                string consulta = @"
                    INSERT INTO Usuarios
                    (Nombre, Apellido, Correo, Contrasena, IdRol)
                    VALUES
                    (@Nombre, @Apellido, @Correo, @Contrasena, @IdRol)";

                using SqlCommand comando = new SqlCommand(consulta, conexion);

                comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value = usuario.Nombre;
                comando.Parameters.Add("@Apellido", SqlDbType.VarChar, 100).Value = usuario.Apellido;
                comando.Parameters.Add("@Correo", SqlDbType.VarChar, 254).Value = usuario.Correo;
                comando.Parameters.Add("@Contrasena", SqlDbType.VarChar, 255).Value = contrasenaHash;
                comando.Parameters.Add("@IdRol", SqlDbType.Int).Value = idRol;

                await comando.ExecuteNonQueryAsync();

                return "Usuario registrado correctamente.";
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                return "Ya existe un usuario registrado con ese correo.";
            }
            catch (Exception ex)
            {
                return "Error al registrar el usuario: " + ex.Message;
            }

        }

        public async Task<Usuario?> IniciarSesion(string correo, string contrasena)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();

            await conexion.OpenAsync();

            string consulta = @"
        SELECT 
            U.IdUsuario,
            U.Nombre,
            U.Apellido,
            U.Correo,
            U.Contrasena,
            U.IdRol,
            R.Nombre AS NombreRol,
            U.Activo
        FROM Usuarios U
        INNER JOIN Roles R ON U.IdRol = R.IdRol
        WHERE U.Correo = @Correo";

            using SqlCommand comando = new SqlCommand(consulta, conexion);

            comando.Parameters.Add("@Correo", SqlDbType.VarChar, 254).Value = correo;

            using SqlDataReader lector = await comando.ExecuteReaderAsync();

            if (!await lector.ReadAsync())
            {
                return null;
            }

            bool activo = Convert.ToBoolean(lector["Activo"]);

            if (!activo)
            {
                return null;
            }

            string hash = lector["Contrasena"].ToString()!;

            bool contrasenaCorrecta = BCrypt.Net.BCrypt.Verify(contrasena, hash);

            if (!contrasenaCorrecta)
            {
                return null;
            }

            Usuario usuario = new Usuario
            {
                IdUsuario = Convert.ToInt32(lector["IdUsuario"]),
                Nombre = lector["Nombre"].ToString()!,
                Apellido = lector["Apellido"].ToString()!,
                Correo = lector["Correo"].ToString()!,
                IdRol = Convert.ToInt32(lector["IdRol"]),
                NombreRol = lector["NombreRol"].ToString()!,
                Activo = activo
            };

            return usuario;
        }

    }

}
