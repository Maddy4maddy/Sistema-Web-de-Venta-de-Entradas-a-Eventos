using Microsoft.Data.SqlClient;
using Sistema_Ventas.Data;
using Sistema_Ventas.Models;
using Sistema_Ventas.ViewModels;
using System.Data;

namespace Sistema_Ventas.Services
{
    public class OrganizadorEventoService
    {
        private readonly ConexionBD conexionBD;

        public OrganizadorEventoService(ConexionBD conexionBD)
        {
            this.conexionBD = conexionBD;
        }

        public async Task<List<Evento>> ObtenerEventosPorOrganizador(
            int idOrganizador)
        {
            var eventos = new List<Evento>();

            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                SELECT IdEvento, Nombre, Descripcion, Categoria, Fecha,
                       Lugar, Ciudad, Imagen, PrecioInicial, Activo,
                       FechaCreacion, IdOrganizador
                FROM Eventos
                WHERE IdOrganizador = @IdOrganizador
                ORDER BY Fecha DESC";

            using SqlCommand comando = new SqlCommand(consulta, conexion);
            comando.Parameters.Add("@IdOrganizador", SqlDbType.Int).Value =
                idOrganizador;

            using SqlDataReader lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                eventos.Add(new Evento
                {
                    IdEvento = Convert.ToInt32(lector["IdEvento"]),
                    Nombre = lector["Nombre"].ToString()!,
                    Descripcion = lector["Descripcion"] == DBNull.Value
                        ? string.Empty
                        : lector["Descripcion"].ToString()!,
                    Categoria = lector["Categoria"].ToString()!,
                    Fecha = Convert.ToDateTime(lector["Fecha"]),
                    Lugar = lector["Lugar"].ToString()!,
                    Ciudad = lector["Ciudad"].ToString()!,
                    Imagen = lector["Imagen"] == DBNull.Value
                        ? string.Empty
                        : lector["Imagen"].ToString()!,
                    PrecioInicial = Convert.ToDecimal(lector["PrecioInicial"]),
                    Activo = Convert.ToBoolean(lector["Activo"]),
                    FechaCreacion = Convert.ToDateTime(lector["FechaCreacion"]),
                    IdOrganizador = Convert.ToInt32(lector["IdOrganizador"])
                });
            }

            return eventos;
        }

        public async Task<EventoOrganizadorViewModel?> ObtenerEventoParaEditar(
            int idEvento,
            int idOrganizador)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consultaEvento = @"
                SELECT IdEvento, Nombre, Descripcion, Categoria, Fecha,
                       Lugar, Ciudad, Imagen, Activo
                FROM Eventos
                WHERE IdEvento = @IdEvento
                  AND IdOrganizador = @IdOrganizador";

            EventoOrganizadorViewModel? modelo = null;

            using (SqlCommand comando = new SqlCommand(consultaEvento, conexion))
            {
                comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                    idEvento;
                comando.Parameters.Add("@IdOrganizador", SqlDbType.Int).Value =
                    idOrganizador;

                using SqlDataReader lector =
                    await comando.ExecuteReaderAsync();

                if (await lector.ReadAsync())
                {
                    modelo = new EventoOrganizadorViewModel
                    {
                        IdEvento = Convert.ToInt32(lector["IdEvento"]),
                        Nombre = lector["Nombre"].ToString()!,
                        Descripcion = lector["Descripcion"] == DBNull.Value
                            ? string.Empty
                            : lector["Descripcion"].ToString()!,
                        Categoria = lector["Categoria"].ToString()!,
                        Fecha = Convert.ToDateTime(lector["Fecha"]),
                        Lugar = lector["Lugar"].ToString()!,
                        Ciudad = lector["Ciudad"].ToString()!,
                        Imagen = lector["Imagen"] == DBNull.Value
                            ? string.Empty
                            : lector["Imagen"].ToString()!
                    };
                }
            }

            if (modelo == null)
            {
                return null;
            }

            modelo.Sectores.Clear();

            string consultaSectores = @"
                SELECT IdSector, Nombre, Precio, Capacidad
                FROM Sectores
                WHERE IdEvento = @IdEvento
                ORDER BY IdSector";

            using (SqlCommand comandoSectores =
                new SqlCommand(consultaSectores, conexion))
            {
                comandoSectores.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                    idEvento;

                using SqlDataReader lector =
                    await comandoSectores.ExecuteReaderAsync();

                while (await lector.ReadAsync())
                {
                    modelo.Sectores.Add(new SectorEventoViewModel
                    {
                        IdSector = Convert.ToInt32(lector["IdSector"]),
                        Nombre = lector["Nombre"].ToString()!,
                        Precio = Convert.ToDecimal(lector["Precio"]),
                        Capacidad = Convert.ToInt32(lector["Capacidad"])
                    });
                }
            }

            return modelo;
        }

        public async Task<int> CrearEvento(
            EventoOrganizadorViewModel modelo,
            int idOrganizador)
        {
            if (!SectoresValidos(modelo.Sectores))
            {
                throw new InvalidOperationException(
                    "Agrega al menos un sector con precio y aforo válidos.");
            }

            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            using SqlTransaction transaccion = conexion.BeginTransaction();

            try
            {
                string consultaEvento = @"
                    INSERT INTO Eventos
                        (Nombre, Descripcion, Categoria, Fecha, Lugar, Ciudad,
                         Imagen, PrecioInicial, Activo, FechaCreacion, IdOrganizador)
                    OUTPUT INSERTED.IdEvento
                    VALUES
                        (@Nombre, @Descripcion, @Categoria, @Fecha, @Lugar, @Ciudad,
                         @Imagen, @PrecioInicial, 0, GETDATE(), @IdOrganizador)";

                int idEvento;

                using (SqlCommand comando = new SqlCommand(
                    consultaEvento, conexion, transaccion))
                {
                    AgregarParametrosEvento(comando, modelo);

                    comando.Parameters.Add("@IdOrganizador", SqlDbType.Int).Value =
                        idOrganizador;

                    idEvento = Convert.ToInt32(
                        await comando.ExecuteScalarAsync());
                }

                foreach (var sector in modelo.Sectores)
                {
                    await InsertarSector(
                        conexion,
                        transaccion,
                        idEvento,
                        sector);
                }

                await transaccion.CommitAsync();
                return idEvento;
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ActualizarEvento(
            EventoOrganizadorViewModel modelo,
            int idOrganizador)
        {
            if (!SectoresValidos(modelo.Sectores))
            {
                return false;
            }

            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            using SqlTransaction transaccion = conexion.BeginTransaction();

            try
            {
                string actualizarEvento = @"
                    UPDATE Eventos
                    SET Nombre = @Nombre,
                        Descripcion = @Descripcion,
                        Categoria = @Categoria,
                        Fecha = @Fecha,
                        Lugar = @Lugar,
                        Ciudad = @Ciudad,
                        Imagen = @Imagen,
                        PrecioInicial = @PrecioInicial
                    WHERE IdEvento = @IdEvento
                      AND IdOrganizador = @IdOrganizador";

                using (SqlCommand comando = new SqlCommand(
                    actualizarEvento, conexion, transaccion))
                {
                    AgregarParametrosEvento(comando, modelo);

                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        modelo.IdEvento;

                    comando.Parameters.Add(
                        "@IdOrganizador", SqlDbType.Int).Value =
                        idOrganizador;

                    if (await comando.ExecuteNonQueryAsync() != 1)
                    {
                        await transaccion.RollbackAsync();
                        return false;
                    }
                }

                var sectoresExistentes = new HashSet<int>();

                string consultaSectores = @"
                    SELECT IdSector
                    FROM Sectores
                    WHERE IdEvento = @IdEvento";

                using (SqlCommand comando = new SqlCommand(
                    consultaSectores, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        modelo.IdEvento;

                    using SqlDataReader lector =
                        await comando.ExecuteReaderAsync();

                    while (await lector.ReadAsync())
                    {
                        sectoresExistentes.Add(
                            Convert.ToInt32(lector["IdSector"]));
                    }
                }

                var idsRecibidos = modelo.Sectores
                    .Where(sector => sector.IdSector > 0)
                    .Select(sector => sector.IdSector)
                    .ToList();

                bool hayIdsRepetidos =
                    idsRecibidos.Count != idsRecibidos.Distinct().Count();

                bool faltanSectoresGuardados =
                    !sectoresExistentes.SetEquals(idsRecibidos);

                bool hayIdsInvalidos =
                    modelo.Sectores.Any(sector => sector.IdSector < 0);

                if (hayIdsRepetidos || faltanSectoresGuardados || hayIdsInvalidos)
                {
                    await transaccion.RollbackAsync();
                    return false;
                }

                foreach (var sector in modelo.Sectores)
                {
                    if (sector.IdSector == 0)
                    {
                        await InsertarSector(
                            conexion,
                            transaccion,
                            modelo.IdEvento,
                            sector);
                    }
                    else
                    {
                        bool actualizado = await ActualizarSector(
                            conexion,
                            transaccion,
                            modelo.IdEvento,
                            sector);

                        if (!actualizado)
                        {
                            await transaccion.RollbackAsync();
                            return false;
                        }
                    }
                }

                await transaccion.CommitAsync();
                return true;
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CambiarEstadoEvento(
            int idEvento,
            int idOrganizador,
            bool publicar)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            string consulta = @"
                UPDATE Eventos
                SET Activo = @Publicar
                WHERE IdEvento = @IdEvento
                  AND IdOrganizador = @IdOrganizador
                  AND (
                        @Publicar = 0
                        OR (
                            Fecha > GETDATE()
                            AND EXISTS (
                                SELECT 1
                                FROM Sectores
                                WHERE Sectores.IdEvento = Eventos.IdEvento
                            )
                        )
                  )";

            using SqlCommand comando = new SqlCommand(consulta, conexion);

            comando.Parameters.Add("@Publicar", SqlDbType.Bit).Value =
                publicar;
            comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                idEvento;
            comando.Parameters.Add("@IdOrganizador", SqlDbType.Int).Value =
                idOrganizador;

            return await comando.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> CancelarEventoConReembolsos(
            int idEvento,
            int idOrganizador)
        {
            using SqlConnection conexion = conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            using SqlTransaction transaccion = conexion.BeginTransaction();

            try
            {
                string cancelarEvento = @"
                    UPDATE Eventos
                    SET Activo = 0
                    WHERE IdEvento = @IdEvento
                      AND IdOrganizador = @IdOrganizador";

                using (SqlCommand comando = new SqlCommand(
                    cancelarEvento, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        idEvento;
                    comando.Parameters.Add("@IdOrganizador", SqlDbType.Int).Value =
                        idOrganizador;

                    if (await comando.ExecuteNonQueryAsync() != 1)
                    {
                        await transaccion.RollbackAsync();
                        return false;
                    }
                }

                string registrarReembolsos = @"
                    INSERT INTO SolicitudesAnulacion
                        (IdCompra, Motivo, Estado, FechaSolicitud,
                         FechaResolucion, MontoReembolso)
                    SELECT
                        C.IdCompra,
                        'Evento cancelado por el organizador',
                        'Aprobada',
                        GETDATE(),
                        GETDATE(),
                        C.Total
                    FROM Compras C
                    INNER JOIN Reservas R ON R.IdReserva = C.IdReserva
                    INNER JOIN Sectores S ON S.IdSector = R.IdSector
                    WHERE S.IdEvento = @IdEvento
                      AND C.Estado = 'Pagada'
                      AND R.Estado = 'Pagada'";

                using (SqlCommand comando = new SqlCommand(
                    registrarReembolsos, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        idEvento;

                    await comando.ExecuteNonQueryAsync();
                }

                string anularEntradas = @"
                    UPDATE T
                    SET T.Estado = 'Anulada'
                    FROM Entradas T
                    INNER JOIN Compras C ON C.IdCompra = T.IdCompra
                    INNER JOIN Reservas R ON R.IdReserva = C.IdReserva
                    INNER JOIN Sectores S ON S.IdSector = R.IdSector
                    WHERE S.IdEvento = @IdEvento
                      AND C.Estado = 'Pagada'
                      AND R.Estado = 'Pagada'";

                using (SqlCommand comando = new SqlCommand(
                    anularEntradas, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        idEvento;

                    await comando.ExecuteNonQueryAsync();
                }

                string anularCompras = @"
                    UPDATE C
                    SET C.Estado = 'Anulada'
                    FROM Compras C
                    INNER JOIN Reservas R ON R.IdReserva = C.IdReserva
                    INNER JOIN Sectores S ON S.IdSector = R.IdSector
                    WHERE S.IdEvento = @IdEvento
                      AND C.Estado = 'Pagada'
                      AND R.Estado = 'Pagada'";

                using (SqlCommand comando = new SqlCommand(
                    anularCompras, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        idEvento;

                    await comando.ExecuteNonQueryAsync();
                }

                string anularReservasPagadas = @"
                    UPDATE R
                    SET R.Estado = 'Anulada'
                    FROM Reservas R
                    INNER JOIN Sectores S ON S.IdSector = R.IdSector
                    WHERE S.IdEvento = @IdEvento
                      AND R.Estado = 'Pagada'";

                using (SqlCommand comando = new SqlCommand(
                    anularReservasPagadas, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        idEvento;

                    await comando.ExecuteNonQueryAsync();
                }

                string rechazarReservasPendientes = @"
                    UPDATE R
                    SET R.Estado = 'Rechazado'
                    FROM Reservas R
                    INNER JOIN Sectores S ON S.IdSector = R.IdSector
                    WHERE S.IdEvento = @IdEvento
                      AND R.Estado = 'Pendiente'";

                using (SqlCommand comando = new SqlCommand(
                    rechazarReservasPendientes, conexion, transaccion))
                {
                    comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                        idEvento;

                    await comando.ExecuteNonQueryAsync();
                }

                await transaccion.CommitAsync();
                return true;
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        private static async Task InsertarSector(
            SqlConnection conexion,
            SqlTransaction transaccion,
            int idEvento,
            SectorEventoViewModel sector)
        {
            string consulta = @"
                INSERT INTO Sectores (IdEvento, Nombre, Precio, Capacidad)
                VALUES (@IdEvento, @Nombre, @Precio, @Capacidad)";

            using SqlCommand comando = new SqlCommand(
                consulta, conexion, transaccion);

            comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                idEvento;
            comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value =
                sector.Nombre.Trim();

            SqlParameter precio = comando.Parameters.Add(
                "@Precio", SqlDbType.Decimal);
            precio.Precision = 10;
            precio.Scale = 2;
            precio.Value = sector.Precio;

            comando.Parameters.Add("@Capacidad", SqlDbType.Int).Value =
                sector.Capacidad;

            await comando.ExecuteNonQueryAsync();
        }

        private static async Task<bool> ActualizarSector(
            SqlConnection conexion,
            SqlTransaction transaccion,
            int idEvento,
            SectorEventoViewModel sector)
        {
            string consulta = @"
                UPDATE S
                SET Nombre = @Nombre,
                    Precio = @Precio,
                    Capacidad = @Capacidad
                FROM Sectores S
                WHERE S.IdSector = @IdSector
                  AND S.IdEvento = @IdEvento
                  AND @Capacidad >= ISNULL((
                      SELECT SUM(R.Cantidad)
                      FROM Reservas R
                      WHERE R.IdSector = S.IdSector
                        AND (
                            (R.Estado = 'Pendiente'
                                AND R.FechaExpiracion > GETDATE())
                            OR R.Estado = 'Pagada'
                        )
                  ), 0)";

            using SqlCommand comando = new SqlCommand(
                consulta, conexion, transaccion);

            comando.Parameters.Add("@IdSector", SqlDbType.Int).Value =
                sector.IdSector;
            comando.Parameters.Add("@IdEvento", SqlDbType.Int).Value =
                idEvento;
            comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value =
                sector.Nombre.Trim();

            SqlParameter precio = comando.Parameters.Add(
                "@Precio", SqlDbType.Decimal);
            precio.Precision = 10;
            precio.Scale = 2;
            precio.Value = sector.Precio;

            comando.Parameters.Add("@Capacidad", SqlDbType.Int).Value =
                sector.Capacidad;

            return await comando.ExecuteNonQueryAsync() == 1;
        }

        private static void AgregarParametrosEvento(
            SqlCommand comando,
            EventoOrganizadorViewModel modelo)
        {
            comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 150).Value =
                modelo.Nombre.Trim();

            comando.Parameters.Add("@Descripcion", SqlDbType.VarChar, 1000).Value =
                string.IsNullOrWhiteSpace(modelo.Descripcion)
                    ? DBNull.Value
                    : modelo.Descripcion.Trim();

            comando.Parameters.Add("@Categoria", SqlDbType.VarChar, 100).Value =
                modelo.Categoria.Trim();

            comando.Parameters.Add("@Fecha", SqlDbType.DateTime2).Value =
                modelo.Fecha;

            comando.Parameters.Add("@Lugar", SqlDbType.VarChar, 200).Value =
                modelo.Lugar.Trim();

            comando.Parameters.Add("@Ciudad", SqlDbType.VarChar, 100).Value =
                modelo.Ciudad.Trim();

            comando.Parameters.Add("@Imagen", SqlDbType.VarChar, 500).Value =
                string.IsNullOrWhiteSpace(modelo.Imagen)
                    ? DBNull.Value
                    : modelo.Imagen.Trim();

            SqlParameter precioInicial = comando.Parameters.Add(
                "@PrecioInicial", SqlDbType.Decimal);
            precioInicial.Precision = 10;
            precioInicial.Scale = 2;
            precioInicial.Value = modelo.Sectores.Min(
                sector => sector.Precio);
        }

        private static bool SectoresValidos(
            List<SectorEventoViewModel> sectores)
        {
            return sectores != null
                && sectores.Count > 0
                && sectores.All(sector =>
                    sector != null
                    && !string.IsNullOrWhiteSpace(sector.Nombre)
                    && sector.Nombre.Length <= 100
                    && sector.Precio > 0
                    && sector.Capacidad > 0);
        }
    }
}
