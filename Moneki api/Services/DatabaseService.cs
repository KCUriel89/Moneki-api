using Microsoft.Data.SqlClient;
using Moneki_api.Controllers;
using Moneki_api.DTOs;
using Moneki_api.Helpers;
using Moneki_api.Models;
using Proyecto_servicio.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace Moneki_api.Services
{
    public abstract class ConnectionToSQL
    {
        private readonly string connectionString =
           "Server=DESKTOP-38IFLSE\\KCSQL;Database=Moneki;Trusted_Connection=True;TrustServerCertificate=True;";
        //escritorio DESKTOP-38IFLSE\KCSQL
        //laptop DESKTOP-N3GOVNS\\KCU_PRUEBA

        protected SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }


    }

    public class DatabaseService : ConnectionToSQL
    {
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        public async Task<TestamentoDetalles> ObtenerDetalleTestamentoAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
    SELECT 
        T.Estado,
        TT.EstadoCivil,
        TT.TieneHijos,
        TT.NumeroHijos,
        TT.BienesDeclarados,
        TT.Pdf,
        U.Nombre + ' ' + U.ApellidoPaterno + ' ' + U.ApellidoMaterno AS NombreCompleto
    FROM Tramites T
    JOIN TramiteTestamento TT ON T.ID_Tramite = TT.ID_Tramite
    JOIN Usuarios U ON T.ID_Usuario = U.ID_Usuario
    WHERE T.ID_Tramite = @id", conn);

            cmd.Parameters.AddWithValue("@id", idTramite);

            using var rd = await cmd.ExecuteReaderAsync();
            if (!rd.Read()) return null;

            return new TestamentoDetalles
            {
                Estado = rd["Estado"].ToString(),
                EstadoCivil = rd["EstadoCivil"].ToString(),
                TieneHijos = (bool)rd["TieneHijos"],
                NumeroHijos = (int)rd["NumeroHijos"],
                BienesDeclarados = rd["BienesDeclarados"].ToString(),
                PdfGenerado = rd["Pdf"] as byte[],
                NombreUsuario = rd["NombreCompleto"].ToString()
            };
        }
        public async Task ActualizarEstadoTramiteINEAsync(int idTramite, string estado)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
UPDATE Tramites
SET Estado = @estado,
    FechaActualizacion = GETDATE()
WHERE ID_Tramite = @id", conn);

            cmd.Parameters.Add("@estado", SqlDbType.VarChar).Value = estado;
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = idTramite;

            int filas = await cmd.ExecuteNonQueryAsync();

            if (filas == 0)
                throw new Exception("No se encontró el trámite.");
        }
        public async Task<string?> ObtenerCorreoUsuarioPorTramiteINEAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
SELECT U.Email
FROM Tramites T
INNER JOIN Usuarios U ON T.ID_Usuario = U.ID_Usuario
INNER JOIN TramiteINE TI ON TI.ID_Tramite = T.ID_Tramite
WHERE T.ID_Tramite = @id", conn);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = idTramite;

            var result = await cmd.ExecuteScalarAsync();

            return result?.ToString();
        }
        public async Task ActualizarEstadoTramiteAsync(int idTramite, string estado)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
    UPDATE Tramites
    SET Estado = @estado,
        FechaActualizacion = GETDATE()
    WHERE ID_Tramite = @id", conn);

            cmd.Parameters.AddWithValue("@estado", estado);
            cmd.Parameters.AddWithValue("@id", idTramite);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RechazarTramiteAsync(int idTramite, string motivo)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
    UPDATE Tramites
    SET Estado = 'Rechazado',
        Observaciones = @motivo,
        FechaActualizacion = GETDATE()
    WHERE ID_Tramite = @id", conn);

            cmd.Parameters.AddWithValue("@motivo", motivo);
            cmd.Parameters.AddWithValue("@id", idTramite);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<TestamentoListaItem>> ObtenerTestamentosParaRevisionAsync()
        {
            var lista = new List<TestamentoListaItem>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT
            t.ID_Tramite,
            t.Estado,
            tt.EstadoCivil
        FROM Tramites t
        INNER JOIN TramiteTestamento tt 
            ON t.ID_Tramite = tt.ID_Tramite
        WHERE t.TipoTramite = 'TESTAMENTO'
          AND t.Estado IN ('Registrado', 'En revisión')";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new TestamentoListaItem
                {
                    IdTramite = reader.GetInt32(0),
                    Estado = reader.GetString(1),
                    EstadoCivil = reader.GetString(2)
                });
            }

            return lista;
        }

        public async Task<List<TestamentoRevisionItem>> ObtenerTestamentosPendientesAsync()
        {
            var lista = new List<TestamentoRevisionItem>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
        SELECT 
            t.ID_Tramite,
            u.Nombre + ' ' + u.ApellidoPaterno + ' ' + u.ApellidoMaterno AS NombreUsuario,
            tt.EstadoCivil,
            tt.TieneHijos,
            tt.NumeroHijos,
            t.Estado,
            t.FechaCreacion
        FROM Tramites t
        INNER JOIN Usuarios u ON u.ID_Usuario = t.ID_Usuario
        INNER JOIN TramiteTestamento tt ON tt.ID_Tramite = t.ID_Tramite
        WHERE t.TipoTramite = 'TESTAMENTO'
        AND t.Estado = 'Registrado'
        ORDER BY t.FechaCreacion DESC", conn);

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                lista.Add(new TestamentoRevisionItem
                {
                    IdTramite = rd.GetInt32(0),
                    NombreUsuario = rd.GetString(1),
                    EstadoCivil = rd.GetString(2),
                    TieneHijos = rd.GetBoolean(3),
                    NumeroHijos = rd.GetInt32(4),
                    Estado = rd.GetString(5),
                    Fecha = rd.GetDateTime(6)
                });
            }

            return lista;
        }
        public async Task<byte[]?> ObtenerPdfAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
        SELECT Pdf
        FROM TramiteTestamento
        WHERE ID_Tramite = @id", conn);

            cmd.Parameters.AddWithValue("@id", idTramite);

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return null;

            return (byte[])result;
        }
        public async Task<List<TestamentoListaItem>> ObtenerTestamentosUsuarioAsync(int idUsuario)
        {
            var lista = new List<TestamentoListaItem>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT
            t.ID_Tramite,
            t.Estado,
            tt.EstadoCivil
        FROM Tramites t
        INNER JOIN TramiteTestamento tt ON t.ID_Tramite = tt.ID_Tramite
        WHERE t.ID_Usuario = @id";

            cmd.Parameters.AddWithValue("@id", idUsuario);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new TestamentoListaItem
                {
                    IdTramite = reader.GetInt32(0),
                    Estado = reader.GetString(1),
                    EstadoCivil = reader.GetString(2)
                });
            }

            return lista;
        }


        public async Task CrearTramiteTestamentoAsync(CrearTestamentoDto dto)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();

            try
            {
                // 1️⃣ Crear trámite general
                var cmdTramite = new SqlCommand(@"
INSERT INTO Tramites (ID_Usuario, TipoTramite, Estado, FechaCreacion)
OUTPUT INSERTED.ID_Tramite
VALUES (@usuario, 'TESTAMENTO', 'Registrado', GETDATE())",
                    conn, tx);

                cmdTramite.Parameters.AddWithValue("@usuario", dto.IdUsuario);

                int idTramite = (int)await cmdTramite.ExecuteScalarAsync();

                // 2️⃣ Generar PDF
                byte[] pdf = TestamentoPdfGenerator.GenerarTestamento(
                    dto.NombreCompleto,
                    dto.EstadoCivil,
                    dto.TieneHijos,
                    dto.NumeroHijos,
                    dto.BienesDeclarados,
                    dto.Fecha
                );

                // 3️⃣ Insertar datos del testamento
                var cmdTestamento = new SqlCommand(@"
INSERT INTO TramiteTestamento
(
    ID_Tramite,
    EstadoCivil,
    TieneHijos,
    NumeroHijos,
    BienesDeclarados,
    Pdf
)
VALUES
(
    @tramite,
    @estado,
    @hijos,
    @numHijos,
    @bienes,
    @pdf
)",
                    conn, tx);

                cmdTestamento.Parameters.AddWithValue("@tramite", idTramite);
                cmdTestamento.Parameters.AddWithValue("@estado", dto.EstadoCivil);
                cmdTestamento.Parameters.AddWithValue("@hijos", dto.TieneHijos);
                cmdTestamento.Parameters.AddWithValue("@numHijos",
                    dto.TieneHijos ? dto.NumeroHijos : 0);
                cmdTestamento.Parameters.AddWithValue("@bienes", dto.BienesDeclarados);
                cmdTestamento.Parameters.Add("@pdf", SqlDbType.VarBinary).Value = pdf;

                await cmdTestamento.ExecuteNonQueryAsync();

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<string?> ObtenerCorreoUsuarioPorTramiteAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
        SELECT u.Email
        FROM Tramites t
        INNER JOIN Usuarios u ON t.ID_Usuario = u.ID_Usuario
        WHERE t.ID_Tramite = @idTramite
    ", conn);

            cmd.Parameters.AddWithValue("@idTramite", idTramite);

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return null;

            return result.ToString();
        }

        public async Task AceptarINEAsync(AceptarIneDto dto)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                // 🔹 1. Actualizar trámite
                var cmd = new SqlCommand(@"
UPDATE Tramites
SET Estado = 'Aceptado',
    ID_Trabajador = @trabajador,
    FechaActualizacion = GETDATE()
WHERE ID_Tramite = @id", conn, transaction);

                cmd.Parameters.AddWithValue("@trabajador", dto.IdTrabajador);
                cmd.Parameters.AddWithValue("@id", dto.IdTramite);

                int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                if (filasAfectadas == 0)
                    throw new Exception("No se encontró el trámite para actualizar.");

                // 🔹 2. Obtener módulo más cercano
                string modulo = await ObtenerModuloMasCercanoAsync(
                    new ModuloCercanoDto
                    {
                        DireccionUsuario = dto.DireccionUsuario
                    });

                // 🔹 3. Confirmar cambios en BD
                await transaction.CommitAsync();

                // 🔹 4. Enviar correo (solo si todo salió bien)
                var emailService = new EmailService();

                await emailService.EnviarCorreoAsync(
                    dto.CorreoUsuario,
                    "Trámite INE Aceptado",
                    $"Tu trámite fue ACEPTADO.\n\nAcude al módulo:\n{modulo}"
                );
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RechazarINEAsync(RechazarIneDto dto)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
UPDATE Tramites
SET Estado = 'Rechazado',
    ID_Trabajador = @trabajador,
    Observaciones = @motivo,
    FechaActualizacion = GETDATE()
WHERE ID_Tramite = @id", conn);

            cmd.Parameters.AddWithValue("@trabajador", dto.IdTrabajador);
            cmd.Parameters.AddWithValue("@motivo", dto.Motivo);
            cmd.Parameters.AddWithValue("@id", dto.IdTramite);

            await cmd.ExecuteNonQueryAsync();

            var emailService = new EmailService();

            await emailService.EnviarCorreoAsync(
                dto.CorreoUsuario,
                "Trámite INE rechazado",
                $@"Tu trámite de INE ha sido RECHAZADO.

Motivo: {dto.Motivo}

Puedes volver a iniciar el trámite desde la app.

MONEKI."
            );
        }


        public async Task<string> ObtenerModuloMasCercanoAsync(ModuloCercanoDto dto)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            string sql = @"
SELECT TOP 1
    Nombre,
    Direccion
FROM ModulosINE";

            using var cmd = new SqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync();

            if (await rd.ReadAsync())
            {
                string nombre = rd.GetString(0);
                string direccion = rd.GetString(1);

                return $"{nombre}\n{direccion}";
            }

            return "No se encontró un módulo INE disponible";
        }

        public async Task<string> ObtenerCorreoUsuarioPorTramite(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            string sql = @"
SELECT u.Email
FROM Tramites t
JOIN Usuarios u ON u.ID_Usuario = t.ID_Usuario
WHERE t.ID_Tramite = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idTramite);

            return (string)await cmd.ExecuteScalarAsync();
        }

        public async Task<List<TramiteINEItem>> ObtenerTramitesINEPendientesAsync()
        {
            var lista = new List<TramiteINEItem>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            string sql = @"
        SELECT 
            t.ID_Tramite,
            t.Estado,
            t.FechaCreacion,
            i.CURP
        FROM Tramites t
        INNER JOIN TramiteINE i ON i.ID_Tramite = t.ID_Tramite
        WHERE t.Estado = 'Registrado'
        ORDER BY t.FechaCreacion DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                lista.Add(new TramiteINEItem
                {
                    IdTramite = rd.GetInt32(0),
                    Estado = rd.GetString(1),
                    FechaCreacion = rd.GetDateTime(2),
                    CURP = rd.GetString(3)
                });
            }

            return lista;
        }

        public async Task ActualizarEstadoTramite(
      int idTramite,
      string estado,
      string observaciones = null)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            string sql = @"
UPDATE Tramites
SET Estado = @e,
    Observaciones = @o,
    FechaActualizacion = GETDATE()
WHERE ID_Tramite = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idTramite);
            cmd.Parameters.AddWithValue("@e", estado);
            cmd.Parameters.AddWithValue("@o", (object?)observaciones ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        /* ===================== USUARIOS ===================== */
        public async Task RegisterUserAsync(
            string nombre,
            string apellidoPaterno,
            string apellidoMaterno,
            string email,
            string password,
            string telefono,
            string direccion,
            DateTime fechaNacimiento,
            DateTime fechaRegistro,
            double latitud,
            double longitud
        )
        {
            string query = @"
        INSERT INTO Usuarios
        (
            Nombre,
            ApellidoPaterno,
            ApellidoMaterno,
            Email,
            PasswordHash,
            Telefono,
            Direccion,
            FechaNacimiento,
            FechaRegistro,
            Latitud,
            Longitud
        )
        VALUES
        (
            @Nombre,
            @ApellidoPaterno,
            @ApellidoMaterno,
            @Email,
            @PasswordHash,
            @Telefono,
            @Direccion,
            @FechaNacimiento,
            @FechaRegistro,
            @Latitud,
            @Longitud
        )";

            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@ApellidoPaterno", apellidoPaterno);
                cmd.Parameters.AddWithValue("@ApellidoMaterno", apellidoMaterno);
                cmd.Parameters.AddWithValue("@Email", email);

                // 🔐 IMPORTANTE: aquí luego puedes meter hashing
                cmd.Parameters.AddWithValue("@PasswordHash", password);

                cmd.Parameters.AddWithValue("@Telefono", telefono ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Direccion", direccion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaNacimiento", fechaNacimiento);
                cmd.Parameters.AddWithValue("@FechaRegistro", fechaRegistro);
                cmd.Parameters.AddWithValue("@Latitud", latitud);
                cmd.Parameters.AddWithValue("@Longitud", longitud);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }


        public async Task<bool> UserExistsAsync(string email)
        {
            const string query = "SELECT COUNT(*) FROM Usuarios WHERE Email = @email";

            using var con = GetConnection();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@email", email);

            await con.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task<(int Id, string Email)?> LoginUsuarioEmailAsync(LoginDto dto)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string query = @"
        SELECT ID_Usuario, Email
        FROM Usuarios
        WHERE Email = @Email AND PasswordHash = @Password";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@Password", dto.Password);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("ID_Usuario"));
                string email = reader.GetString(reader.GetOrdinal("Email"));

                return (id, email);
            }

            return null;
        }


        public async Task<bool> CorreoExisteUsuariosAsync(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return false;

            correo = correo.Trim().ToLower();

            string query = @"
SELECT COUNT(*)
FROM Usuarios
WHERE LOWER(LTRIM(RTRIM(Email))) = @correo";

            using SqlConnection con = GetConnection();
            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@correo", correo);

            await con.OpenAsync();
            int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return count > 0;
        }



        /* ===================== TRABAJADORES ===================== */

        public async Task<(int Id, string Email)?> LoginTrabajadorEmailAsync(LoginDto dto)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string query = @"
        SELECT ID_Trabajador, Email
        FROM Trabajadores
        WHERE Email = @Email AND PasswordHash = @Password";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@Password", dto.Password);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("ID_Trabajador"));
                string email = reader.GetString(reader.GetOrdinal("Email"));

                return (id, email);
            }

            return null;
        }


        /* ===================== ADMINISTRADORES ===================== */

        public async Task<(int Id, string Email)?> LoginAdminEmailAsync(LoginDto dto)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string query = @"
        SELECT ID_Administrador, Email
        FROM Administradores
        WHERE Email = @Email AND PasswordHash = @Password";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@Password", dto.Password);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("ID_Administrador"));
                string email = reader.GetString(reader.GetOrdinal("Email"));

                return (id, email);
            }

            return null;
        }



        /* ===================== RECUPERACIÓN PASSWORD ===================== */
        public async Task<int> ActualizarPasswordPorCorreoAsync(string correo, string nuevaPassword)
        {
            string query = @"
        UPDATE Usuarios
        SET PasswordHash = @pw
        WHERE Email = @correo
    ";

            using SqlConnection con = GetConnection();
            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@pw", nuevaPassword);
            cmd.Parameters.AddWithValue("@correo", correo);

            await con.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task GuardarCodigoRecuperacionAsync(string correo, string codigo)
        {
            const string query = @"
                INSERT INTO RecuperacionPassword (Correo, Codigo)
                VALUES (@Correo, @Codigo)";

            await ExecuteAsync(query,
                new SqlParameter("@Correo", correo),
                new SqlParameter("@Codigo", codigo));
        }

        public async Task<bool> ValidarCodigoAsync(string correo, string codigo)
        {
            const string query = @"
                SELECT COUNT(*) FROM RecuperacionPassword
                WHERE Correo = @Correo AND Codigo = @Codigo AND Usado = 0";

            using var con = GetConnection();
            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Correo", correo);
            cmd.Parameters.AddWithValue("@Codigo", codigo);

            await con.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task MarcarCodigoUsadoAsync(string correo, string codigo)
        {
            const string query = @"
                UPDATE RecuperacionPassword
                SET Usado = 1
                WHERE Correo = @Correo AND Codigo = @Codigo";

            await ExecuteAsync(query,
                new SqlParameter("@Correo", correo),
                new SqlParameter("@Codigo", codigo));
        }

        public async Task ActualizarPasswordUsuarioAsync(string correo, string nuevoPasswordHash)
        {
            const string query = @"
                UPDATE Usuarios
                SET PasswordHash = @PasswordHash
                WHERE Email = @Email";

            await ExecuteAsync(query,
                new SqlParameter("@PasswordHash", nuevoPasswordHash),
                new SqlParameter("@Email", correo));
        }

        /* ===================== HELPERS ===================== */

        private async Task<Dictionary<string, object>?> EjecutarLoginAsync(
            string query, params SqlParameter[] parameters)
        {
            using var con = GetConnection();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddRange(parameters);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var result = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                result[reader.GetName(i)] = reader.GetValue(i);

            return result;
        }

        public async Task<int> ExecuteAsync(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddRange(parameters);
                await con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }
        public async Task<List<ModuloINE>> ObtenerModulosINEAsync()
        {
            string query = @"
        SELECT IdModulo, Nombre, Direccion, Latitud, Longitud
        FROM ModulosAtencion
        WHERE TipoModulo = 'INE'";

            var lista = new List<ModuloINE>();

            using SqlConnection con = GetConnection();
            using SqlCommand cmd = new SqlCommand(query, con);

            await con.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new ModuloINE
                {
                    IdModulo = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Direccion = reader.GetString(2),
                    Latitud = reader.GetDouble(3),
                    Longitud = reader.GetDouble(4),
                    DistanciaKm = 0
                });
            }

            return lista;
        }

        // ===================== Tramites =====================

        public async Task<INECompleto?> ObtenerINECompleto(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            string sql = @"
SELECT 
    t.ID_Tramite,
    i.CURP,
    t.Estado,
    t.FechaCreacion,
    i.ActaNacimiento,
    i.ComprobanteDomicilio,
    i.Identificacion,
    u.Email,
    u.Direccion
FROM Tramites t
INNER JOIN TramiteINE i ON i.ID_Tramite = t.ID_Tramite
INNER JOIN Usuarios u ON u.ID_Usuario = t.ID_Usuario
WHERE t.ID_Tramite = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idTramite);

            using var rd = await cmd.ExecuteReaderAsync();

            if (!await rd.ReadAsync())
                return null;

            return new INECompleto
            {
                IdTramite = rd.GetInt32(0),
                CURP = rd.IsDBNull(1) ? "" : rd.GetString(1),
                Estado = rd.IsDBNull(2) ? "" : rd.GetString(2),
                Fecha = rd.GetDateTime(3),

                ActaNacimiento = rd.IsDBNull(4)
    ? null
    : await rd.GetFieldValueAsync<byte[]>(4),

                ComprobanteDomicilio = rd.IsDBNull(5)
    ? null
    : await rd.GetFieldValueAsync<byte[]>(5),

                Identificacion = rd.IsDBNull(6)
    ? null
    : await rd.GetFieldValueAsync<byte[]>(6),

                CorreoUsuario = rd.IsDBNull(7) ? "" : rd.GetString(7),
                DireccionUsuario = rd.IsDBNull(8) ? "" : rd.GetString(8)
            };
        }






        public async Task<List<TramiteINEItem>> ObtenerMisTramitesINEAsync(int IdUsuario)
        {
            List<TramiteINEItem> lista = new();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
        SELECT 
            t.ID_Tramite,
            i.CURP,
            t.Estado,
            t.FechaCreacion
        FROM Tramites t
        INNER JOIN TramiteINE i ON i.ID_Tramite = t.ID_Tramite
        WHERE t.ID_Usuario = @id", conn);

            cmd.Parameters.AddWithValue("@id", IdUsuario);

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                lista.Add(new TramiteINEItem
                {
                    IdTramite = rd.GetInt32(0),
                    CURP = rd.GetString(1),
                    Estado = rd.GetString(2),
                    FechaCreacion = rd.GetDateTime(3)
                });
            }

            return lista;
        }


        public async Task CrearTramiteINEAsync(CrearTramiteINEDto dto)
        {
            using SqlConnection conn = GetConnection();
            await conn.OpenAsync();

            using SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                string insertTramite = @"
INSERT INTO Tramites (ID_Usuario, TipoTramite)
OUTPUT INSERTED.ID_Tramite
VALUES (@ID_Usuario, 'INE')";

                int idTramite;

                using (SqlCommand cmd = new SqlCommand(insertTramite, conn, transaction))
                {
                    cmd.Parameters.Add("@ID_Usuario", SqlDbType.Int).Value = dto.IdUsuario;
                    idTramite = (int)await cmd.ExecuteScalarAsync();
                }

                string insertINE = @"
INSERT INTO TramiteINE
(ID_Tramite, CURP, ActaNacimiento, ComprobanteDomicilio, Identificacion)
VALUES
(@ID_Tramite, @CURP, @Acta, @Comprobante, @Identificacion)";

                using (SqlCommand cmd = new SqlCommand(insertINE, conn, transaction))
                {
                    cmd.Parameters.Add("@ID_Tramite", SqlDbType.Int).Value = idTramite;
                    cmd.Parameters.Add("@CURP", SqlDbType.VarChar, 18).Value = dto.CURP;

                    cmd.Parameters.Add("@Acta", SqlDbType.VarBinary).Value = dto.ActaNacimiento;
                    cmd.Parameters.Add("@Comprobante", SqlDbType.VarBinary).Value = dto.ComprobanteDomicilio;
                    cmd.Parameters.Add("@Identificacion", SqlDbType.VarBinary).Value = dto.Identificacion;

                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }



        public async Task<List<TramiteModel>> GetTramitesUsuarioAsync(int idUsuario)
        {
            var lista = new List<TramiteModel>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            string query = @"
SELECT 
    ID_Tramite,
    TipoTramite,
    Estado,
    FechaCreacion
FROM Tramites
WHERE ID_Usuario = @id
ORDER BY FechaCreacion DESC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", idUsuario);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new TramiteModel
                {
                    ID_Tramite = reader.GetInt32(0),
                    TipoTramite = reader.GetString(1),
                    Estado = reader.GetString(2),
                    FechaCreacion = reader.GetDateTime(3)
                });
            }

            return lista;
        }



        public async Task<List<UsuarioItem>> ObtenerUsuariosAsync()
        {
            var lista = new List<UsuarioItem>();

            using var con = GetConnection();

            string q = @"
SELECT 
    ID_Usuario,
    Nombre + ' ' + ApellidoPaterno AS NombreCompleto,
    Email
FROM Usuarios";

            using var cmd = new SqlCommand(q, con);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                lista.Add(new UsuarioItem
                {
                    ID_Usuario = rd.GetInt32(0),
                    NombreCompleto = rd.GetString(1),
                    Email = rd.GetString(2)
                });
            }

            return lista;
        }

        public async Task CrearTramiteCompraventaAsync(CrearTramiteCompraventaDto dto)
        {
            using SqlConnection conn = GetConnection();
            await conn.OpenAsync();

            using SqlTransaction tx = conn.BeginTransaction();

            try
            {
                // 1️⃣ Crear registro en Tramites
                string qTramite = @"
INSERT INTO Tramites (ID_Usuario, TipoTramite)
OUTPUT INSERTED.ID_Tramite
VALUES (@ID_Usuario, 'COMPRAVENTA')";

                int idTramite;

                using (SqlCommand cmd = new SqlCommand(qTramite, conn, tx))
                {
                    cmd.Parameters.Add("@ID_Usuario", SqlDbType.Int)
                        .Value = dto.IdUsuario;

                    idTramite = (int)await cmd.ExecuteScalarAsync();
                }

                // 2️⃣ Generar PDF en backend
                byte[] contratoGenerado = ContratoPdfGenerator.GenerarContrato(
                    dto.Vendedor,
                    dto.Comprador,
                    dto.TipoBien,
                    dto.Monto
                );

                // 3️⃣ Insertar en TramiteCompraventa
                string qCompra = @"
INSERT INTO TramiteCompraventa
(
    ID_Tramite, 
    TipoBien, 
    Vendedor, 
    Comprador, 
    Monto, 
    ContratoPDF, 
    IdentificacionVendedor, 
    IdentificacionComprador
)
VALUES
(
    @ID_Tramite, 
    @TipoBien, 
    @Vendedor, 
    @Comprador, 
    @Monto, 
    @ContratoPDF, 
    @IdVendedor, 
    @IdComprador
)";

                using (SqlCommand cmd = new SqlCommand(qCompra, conn, tx))
                {
                    cmd.Parameters.Add("@ID_Tramite", SqlDbType.Int)
                        .Value = idTramite;

                    cmd.Parameters.Add("@TipoBien", SqlDbType.VarChar, 30)
                        .Value = dto.TipoBien ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@Vendedor", SqlDbType.NVarChar, 150)
                        .Value = dto.Vendedor ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@Comprador", SqlDbType.NVarChar, 150)
                        .Value = dto.Comprador ?? (object)DBNull.Value;

                    var paramMonto = cmd.Parameters.Add("@Monto", SqlDbType.Decimal);
                    paramMonto.Precision = 18;
                    paramMonto.Scale = 2;
                    paramMonto.Value = dto.Monto;

                    cmd.Parameters.Add("@ContratoPDF", SqlDbType.VarBinary)
                        .Value = contratoGenerado ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@IdVendedor", SqlDbType.VarBinary)
                        .Value = dto.IdentificacionVendedor ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@IdComprador", SqlDbType.VarBinary)
                        .Value = dto.IdentificacionComprador ?? (object)DBNull.Value;

                    await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        public async Task<List<ContratoItemDto>> ObtenerMisContratosCompreventaAsync(int idUsuario)
        {
            var lista = new List<ContratoItemDto>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
        SELECT ID_Tramite, TipoTramite, Estado, FechaCreacion
        FROM Tramites
        WHERE ID_Usuario = @id
        AND TipoTramite IN ('COMPRAVENTA')
        ORDER BY FechaCreacion DESC", conn);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;

            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                lista.Add(new ContratoItemDto
                {
                    ID_Tramite = rd.GetInt32(0),
                    TipoTramite = rd.GetString(1),
                    Estado = rd.IsDBNull(2) ? "" : rd.GetString(2),
                    FechaCreacion = rd.GetDateTime(3)
                });
            }

            return lista;
        }
        public async Task<List<ContratoItemDto>> ObtenerMisTestamentosAsync(int idUsuario)
        {
            var lista = new List<ContratoItemDto>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
        SELECT ID_Tramite, TipoTramite, Estado, FechaCreacion
        FROM Tramites
        WHERE ID_Usuario = @id
        AND TipoTramite IN ('TESTAMENTO')
        ORDER BY FechaCreacion DESC", conn);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;

            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                lista.Add(new ContratoItemDto
                {
                    ID_Tramite = rd.GetInt32(0),
                    TipoTramite = rd.GetString(1),
                    Estado = rd.IsDBNull(2) ? "" : rd.GetString(2),
                    FechaCreacion = rd.GetDateTime(3)
                });
            }

            return lista;
        }

        public async Task<ContratoCompletoDto> ObtenerContratoCompletoAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            string sql = @" 
SELECT 
    t.ID_Tramite,
    t.TipoTramite,
    t.Estado,
    t.Observaciones,
    t.FechaCreacion,

    i.CURP,
    i.ActaNacimiento,
    i.ComprobanteDomicilio,
    i.Identificacion,

    c.Vendedor,
    c.Comprador,
    c.TipoBien,
    c.Monto,
    c.ContratoPDF,
    c.IdentificacionVendedor,
    c.IdentificacionComprador,

    te.EstadoCivil,
    te.TieneHijos,
    te.NumeroHijos,
    te.BienesDeclarados,

    s.TipoSucesion,
    s.NombreFallecido,
    s.FechaDefuncion,
    s.NumeroHerederos

FROM Tramites t
LEFT JOIN TramiteINE i ON i.ID_Tramite = t.ID_Tramite
LEFT JOIN TramiteCompraventa c ON c.ID_Tramite = t.ID_Tramite
LEFT JOIN TramiteTestamento te ON te.ID_Tramite = t.ID_Tramite
LEFT JOIN TramiteSucesion s ON s.ID_Tramite = t.ID_Tramite
WHERE t.ID_Tramite = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = idTramite;

            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;

            var contrato = new ContratoCompletoDto
            {
                IdTramite = rd.GetInt32(0),
                TipoTramite = rd.GetString(1),
                Estado = rd.IsDBNull(2) ? "" : rd.GetString(2),
                Observaciones = rd.IsDBNull(3) ? "" : rd.GetString(3),
                Fecha = rd.GetDateTime(4),

                CURP = rd.IsDBNull(5) ? null : rd.GetString(5),
                ActaNacimientoBase64 = rd.IsDBNull(6) ? null : Convert.ToBase64String((byte[])rd[6]),
                ComprobanteDomicilioBase64 = rd.IsDBNull(7) ? null : Convert.ToBase64String((byte[])rd[7]),
                IdentificacionBase64 = rd.IsDBNull(8) ? null : Convert.ToBase64String((byte[])rd[8]),

                Vendedor = rd.IsDBNull(9) ? null : rd.GetString(9),
                Comprador = rd.IsDBNull(10) ? null : rd.GetString(10),
                TipoBien = rd.IsDBNull(11) ? null : rd.GetString(11),
                Monto = rd.IsDBNull(12) ? null : rd.GetDecimal(12),
                ContratoPDFBase64 = rd.IsDBNull(13) ? null : Convert.ToBase64String((byte[])rd[13]),
                IdentificacionVendedorBase64 = rd.IsDBNull(14) ? null : Convert.ToBase64String((byte[])rd[14]),
                IdentificacionCompradorBase64 = rd.IsDBNull(15) ? null : Convert.ToBase64String((byte[])rd[15]),

                EstadoCivil = rd.IsDBNull(16) ? null : rd.GetString(16),
                TieneHijos = rd.IsDBNull(17) ? null : rd.GetBoolean(17),
                NumeroHijos = rd.IsDBNull(18) ? null : rd.GetInt32(18),
                BienesDeclarados = rd.IsDBNull(19) ? null : rd.GetString(19),

                TipoSucesion = rd.IsDBNull(20) ? null : rd.GetString(20),
                NombreFallecido = rd.IsDBNull(21) ? null : rd.GetString(21),
                FechaDefuncion = rd.IsDBNull(22) ? null : rd.GetDateTime(22),
                NumeroHerederos = rd.IsDBNull(23) ? null : rd.GetInt32(23)
            };

            return contrato;
        }
        public async Task<List<CompraventaDetalleDto>> ObtenerPendientesAsync()
        {
            var lista = new List<CompraventaDetalleDto>();

            using var conn = GetConnection();

            string query = @"
SELECT t.ID_Tramite, c.ID_Compraventa,
       c.TipoBien, c.Monto,
       c.Comprador, c.Vendedor,
       c.IdentificacionVendedor,
       c.IdentificacionComprador,
       c.ContratoPDF,
       u.Email
FROM dbo.Tramites t
INNER JOIN dbo.TramiteCompraventa c ON t.ID_Tramite = c.ID_Tramite
INNER JOIN dbo.Usuarios u ON t.ID_Usuario = u.ID_Usuario
WHERE t.Estado = @estado";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@estado", "Registrado");

            await conn.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new CompraventaDetalleDto
                {
                    ID_Tramite = (int)reader["ID_Tramite"],
                    ID_Compraventa = (int)reader["ID_Compraventa"],
                    TipoBien = reader["TipoBien"].ToString(),
                    Monto = (decimal)reader["Monto"],
                    Comprador = reader["Comprador"].ToString(),
                    Vendedor = reader["Vendedor"].ToString(),
                    IdentificacionVendedor = reader["IdentificacionVendedor"] as byte[],
                    IdentificacionComprador = reader["IdentificacionComprador"] as byte[],
                    ContratoPDF = reader["ContratoPDF"] as byte[],
                    CorreoUsuario = reader["Email"].ToString()
                });
            }

            return lista;
        }

        // ===============================
        // 🔹 OBTENER DETALLE
        // ===============================
        public async Task<CompraventaDetalleDto> ObtenerDetalleAsync(int idTramite)
        {
            using var conn = GetConnection();

            string query = @"
SELECT t.ID_Tramite, c.ID_Compraventa,
       c.TipoBien, c.Monto,
       c.Comprador, c.Vendedor,
       c.IdentificacionVendedor,
       c.IdentificacionComprador,
       c.ContratoPDF,
       u.Email
FROM dbo.Tramites t
INNER JOIN dbo.TramiteCompraventa c ON t.ID_Tramite = c.ID_Tramite
INNER JOIN dbo.Usuarios u ON t.ID_Usuario = u.ID_Usuario
WHERE t.ID_Tramite = @id";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", idTramite);

            await conn.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new CompraventaDetalleDto
                {
                    ID_Tramite = (int)reader["ID_Tramite"],
                    ID_Compraventa = (int)reader["ID_Compraventa"],
                    TipoBien = reader["TipoBien"].ToString(),
                    Monto = (decimal)reader["Monto"],
                    Comprador = reader["Comprador"].ToString(),
                    Vendedor = reader["Vendedor"].ToString(),
                    IdentificacionVendedor = reader["IdentificacionVendedor"] as byte[],
                    IdentificacionComprador = reader["IdentificacionComprador"] as byte[],
                    ContratoPDF = reader["ContratoPDF"] as byte[],
                    CorreoUsuario = reader["Email"].ToString()
                };
            }

            return null;
        }

        // ===============================
        // 🔹 ACEPTAR / RECHAZAR
        // ===============================
        public async Task CambiarEstadoAsync(int idTramite, string nuevoEstado)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // 1️⃣ Actualizar estado
            string update = @"UPDATE Tramites
                          SET Estado = @estado,
                              FechaActualizacion = GETDATE()
                          WHERE ID_Tramite = @id";

            using SqlCommand cmd = new SqlCommand(update, conn);
            cmd.Parameters.AddWithValue("@estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@id", idTramite);

            await cmd.ExecuteNonQueryAsync();

            // 2️⃣ Obtener correo
            string correoQuery = @"
            SELECT u.Email
            FROM Tramites t
            INNER JOIN Usuarios u ON t.ID_Usuario = u.ID_Usuario
            WHERE t.ID_Tramite = @id";

            using SqlCommand cmdCorreo = new SqlCommand(correoQuery, conn);
            cmdCorreo.Parameters.AddWithValue("@id", idTramite);

            string correo = (string)await cmdCorreo.ExecuteScalarAsync();

            // 3️⃣ Enviar correo usando TU EmailService
            string asunto = "Resultado de tu contrato de compraventa";
            string mensaje = $"Tu contrato fue {nuevoEstado} correctamente.";
            var emailService = new EmailService();

            await emailService.EnviarCorreoAsync(correo, asunto, mensaje);
        }

        // ===================== Funciones Admin =====================

        public async Task<List<TrabajadorItemDto>> ObtenerTrabajadoresAsync()
        {
            var lista = new List<TrabajadorItemDto>();

            using var con = GetConnection();

            string q = @"
        SELECT 
            ID_Trabajador,
            CONCAT(Nombre,' ',ApellidoPaterno) AS NombreCompleto,
            Email
        FROM Trabajadores
        ORDER BY Nombre";

            using var cmd = new SqlCommand(q, con);

            await con.OpenAsync();

            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                lista.Add(new TrabajadorItemDto
                {
                    ID_Trabajador = rd.GetInt32(0),
                    NombreCompleto = rd.IsDBNull(1) ? "" : rd.GetString(1),
                    Email = rd.IsDBNull(2) ? "" : rd.GetString(2)
                });
            }

            return lista;
        }

        //eliminar usuarios
        public async Task<bool> EliminarUsuarioAsync(int id)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            // 1️⃣ Eliminar TramiteINE (hijos de Tramites)
            string q1 = @"
        DELETE FROM TramiteINE
        WHERE ID_Tramite IN (
            SELECT ID_Tramite FROM Tramites WHERE ID_Usuario = @id
        )";

            using (var cmd1 = new SqlCommand(q1, con))
            {
                cmd1.Parameters.AddWithValue("@id", id);
                await cmd1.ExecuteNonQueryAsync();
            }

            // 2️⃣ Eliminar Tramites
            string q2 = "DELETE FROM Tramites WHERE ID_Usuario = @id";

            using (var cmd2 = new SqlCommand(q2, con))
            {
                cmd2.Parameters.AddWithValue("@id", id);
                await cmd2.ExecuteNonQueryAsync();
            }

            // 3️⃣ Eliminar Usuario
            string q3 = "DELETE FROM Usuarios WHERE ID_Usuario = @id";

            using (var cmd3 = new SqlCommand(q3, con))
            {
                cmd3.Parameters.AddWithValue("@id", id);
                int filas = await cmd3.ExecuteNonQueryAsync();
                return filas > 0;
            }
        }

        public async Task<bool> EliminarTrabajadorAsync(int id)
        {
            using var con = GetConnection();

            string q = "DELETE FROM Trabajadores WHERE ID_Trabajador = @id";

            using var cmd = new SqlCommand(q, con);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

            await con.OpenAsync();

            int filasAfectadas = await cmd.ExecuteNonQueryAsync();

            return filasAfectadas > 0;
        }

        //agregar trabajador
        public async Task<bool> TrabajadorExisteAsync(string email)
        {
            string query = @"
        IF EXISTS (SELECT 1 FROM Trabajadores WHERE Email = @e)
            SELECT 1
        ELSE
            SELECT 0";

            using var con = GetConnection();
            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.Add("@e", SqlDbType.VarChar, 150).Value = email;

            await con.OpenAsync();

            int result = (int)await cmd.ExecuteScalarAsync();

            return result == 1;
        }
        public async Task<int> InsertarTrabajadorAsync(CrearTrabajadorDto dto)
        {
            string query = @"
        INSERT INTO Trabajadores
        (Nombre, ApellidoPaterno, ApellidoMaterno, Email, Cargo, Departamento, PasswordHash)
        OUTPUT INSERTED.ID_Trabajador
        VALUES
        (@n, @ap, @am, @e, @c, @d, @p)";

            using var con = GetConnection();
            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.Add("@n", SqlDbType.VarChar, 100).Value = dto.Nombre;
            cmd.Parameters.Add("@ap", SqlDbType.VarChar, 100).Value = dto.ApellidoPaterno;
            cmd.Parameters.Add("@am", SqlDbType.VarChar, 100).Value = dto.ApellidoMaterno;
            cmd.Parameters.Add("@e", SqlDbType.VarChar, 150).Value = dto.Email;
            cmd.Parameters.Add("@c", SqlDbType.VarChar, 100).Value = dto.Cargo;
            cmd.Parameters.Add("@d", SqlDbType.VarChar, 100).Value = dto.Departamento;

            // 🔥 Guardar contraseña directa (SIN HASH)
            cmd.Parameters.Add("@p", SqlDbType.VarChar, 500).Value = dto.Password;

            await con.OpenAsync();

            int idGenerado = (int)await cmd.ExecuteScalarAsync();

            return idGenerado;
        }
    }

}

