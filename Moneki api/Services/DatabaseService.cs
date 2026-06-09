using Npgsql;
using NpgsqlTypes;
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
using System.Threading.Tasks;
#pragma warning disable CS8601, CS8603, CS8604, CS8605, CS8625
namespace Moneki_api.Services
{
   public abstract class ConnectionToSQL
    {
        private readonly string _connectionString;

protected ConnectionToSQL()
{
    var connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("SUPABASE_CONNEXION_STRING no configurada");
    }
    
    // Limpiar comillas
    connectionString = connectionString.Trim().Trim('"', '\'');
    
    // 🔥 ELIMINAR Host Resolver (no es válido para Npgsql)
    if (connectionString.Contains("Host Resolver"))
    {
        // Opción 1: Remover completamente
        connectionString = System.Text.RegularExpressions.Regex.Replace(
            connectionString, 
            ";Host Resolver=[^;]*", 
            "");
        
        // Opción 2: Alternativa más simple
        // connectionString = connectionString.Replace(";Host Resolver=PreferIPv4", "");
    }
    
    _connectionString = connectionString;
    
    // Log seguro
    var logString = System.Text.RegularExpressions.Regex.Replace(
        _connectionString, 
        "Password=[^;]*", 
        "Password=***");
    Console.WriteLine($"✅ Connection configured: {logString}");
}
        protected NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
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

        // ===================== TESTAMENTOS =====================

        public async Task<TestamentoDetalles> ObtenerDetalleTestamentoAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
    SELECT 
        T.Estado,
        TT.EstadoCivil,
        TT.TieneHijos,
        TT.NumeroHijos,
        TT.BienesDeclarados,
        TT.Pdf,
        U.Nombre || ' ' || U.ApellidoPaterno || ' ' || U.ApellidoMaterno AS NombreCompleto
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

            var cmd = new NpgsqlCommand(@"
UPDATE Tramites
SET Estado = @estado,
    FechaActualizacion = NOW()
WHERE ID_Tramite = @id", conn);

            cmd.Parameters.Add("@estado", NpgsqlDbType.Varchar).Value = estado;
            cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = idTramite;

            int filas = await cmd.ExecuteNonQueryAsync();

            if (filas == 0)
                throw new Exception("No se encontró el trámite.");
        }

        public async Task<string?> ObtenerCorreoUsuarioPorTramiteINEAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
SELECT U.Email
FROM Tramites T
INNER JOIN Usuarios U ON T.ID_Usuario = U.ID_Usuario
INNER JOIN TramiteINE TI ON TI.ID_Tramite = T.ID_Tramite
WHERE T.ID_Tramite = @id", conn);

            cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = idTramite;

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        public async Task ActualizarEstadoTramiteAsync(int idTramite, string estado)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
    UPDATE Tramites
    SET Estado = @estado,
        FechaActualizacion = NOW()
    WHERE ID_Tramite = @id", conn);

            cmd.Parameters.AddWithValue("@estado", estado);
            cmd.Parameters.AddWithValue("@id", idTramite);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RechazarTramiteAsync(int idTramite, string motivo)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
    UPDATE Tramites
    SET Estado = 'Rechazado',
        Observaciones = @motivo,
        FechaActualizacion = NOW()
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

            var cmd = new NpgsqlCommand(@"
        SELECT
            t.ID_Tramite,
            t.Estado,
            tt.EstadoCivil
        FROM Tramites t
        INNER JOIN TramiteTestamento tt 
            ON t.ID_Tramite = tt.ID_Tramite
        WHERE t.TipoTramite = 'TESTAMENTO'
          AND t.Estado IN ('Registrado', 'En revisión')", conn);

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

            var cmd = new NpgsqlCommand(@"
        SELECT 
            t.ID_Tramite,
            u.Nombre || ' ' || u.ApellidoPaterno || ' ' || u.ApellidoMaterno AS NombreUsuario,
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

            var cmd = new NpgsqlCommand(@"
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

            var cmd = new NpgsqlCommand(@"
        SELECT
            t.ID_Tramite,
            t.Estado,
            tt.EstadoCivil
        FROM Tramites t
        INNER JOIN TramiteTestamento tt ON t.ID_Tramite = tt.ID_Tramite
        WHERE t.ID_Usuario = @id", conn);

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

            using var tx = await conn.BeginTransactionAsync();

            try
            {
                var cmdTramite = new NpgsqlCommand(@"
INSERT INTO Tramites (ID_Usuario, TipoTramite, Estado, FechaCreacion)
VALUES (@usuario, 'TESTAMENTO', 'Registrado', NOW())
RETURNING ID_Tramite", conn, tx);

                cmdTramite.Parameters.AddWithValue("@usuario", dto.IdUsuario);

                int idTramite = (int)await cmdTramite.ExecuteScalarAsync();

                byte[] pdf = TestamentoPdfGenerator.GenerarTestamento(
                    dto.NombreCompleto,
                    dto.EstadoCivil,
                    dto.TieneHijos,
                    dto.NumeroHijos,
                    dto.BienesDeclarados,
                    dto.Fecha
                );

                var cmdTestamento = new NpgsqlCommand(@"
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
)", conn, tx);

                cmdTestamento.Parameters.AddWithValue("@tramite", idTramite);
                cmdTestamento.Parameters.AddWithValue("@estado", dto.EstadoCivil);
                cmdTestamento.Parameters.AddWithValue("@hijos", dto.TieneHijos);
                cmdTestamento.Parameters.AddWithValue("@numHijos",
                    dto.TieneHijos ? dto.NumeroHijos : 0);
                cmdTestamento.Parameters.AddWithValue("@bienes", dto.BienesDeclarados);
                cmdTestamento.Parameters.Add("@pdf", NpgsqlDbType.Bytea).Value = pdf;

                await cmdTestamento.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<string?> ObtenerCorreoUsuarioPorTramiteAsync(int idTramite)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
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

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                var cmd = new NpgsqlCommand(@"
UPDATE Tramites
SET Estado = 'Aceptado',
    ID_Trabajador = @trabajador,
    FechaActualizacion = NOW()
WHERE ID_Tramite = @id", conn, transaction);

                cmd.Parameters.AddWithValue("@trabajador", dto.IdTrabajador);
                cmd.Parameters.AddWithValue("@id", dto.IdTramite);

                int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                if (filasAfectadas == 0)
                    throw new Exception("No se encontró el trámite para actualizar.");

                string modulo = await ObtenerModuloMasCercanoAsync(
                    new ModuloCercanoDto
                    {
                        DireccionUsuario = dto.DireccionUsuario
                    });

                await transaction.CommitAsync();

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

            var cmd = new NpgsqlCommand(@"
UPDATE Tramites
SET Estado = 'Rechazado',
    ID_Trabajador = @trabajador,
    Observaciones = @motivo,
    FechaActualizacion = NOW()
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
SELECT
    Nombre,
    Direccion
FROM ModulosINE
LIMIT 1";

            using var cmd = new NpgsqlCommand(sql, conn);
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

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idTramite);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? "";
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

            using var cmd = new NpgsqlCommand(sql, conn);
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
    FechaActualizacion = NOW()
WHERE ID_Tramite = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idTramite);
            cmd.Parameters.AddWithValue("@e", estado);
            cmd.Parameters.AddWithValue("@o", observaciones ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        // ===================== USUARIOS =====================

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
            string hashedPassword = HashPassword(password);
            
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

            using (NpgsqlConnection con = GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@ApellidoPaterno", apellidoPaterno);
                cmd.Parameters.AddWithValue("@ApellidoMaterno", apellidoMaterno);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
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
            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@email", email);

            await con.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task<(int Id, string Email)?> LoginUsuarioEmailAsync(LoginDto dto)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string hashedPassword = HashPassword(dto.Password);
            
            string query = @"
        SELECT ID_Usuario, Email
        FROM Usuarios
        WHERE Email = @Email AND PasswordHash = @Password";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@Password", hashedPassword);

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
WHERE LOWER(Email) = @correo";

            using NpgsqlConnection con = GetConnection();
            using NpgsqlCommand cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.AddWithValue("@correo", correo);

            await con.OpenAsync();
            int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return count > 0;
        }

        // ===================== TRABAJADORES =====================

        public async Task<(int Id, string Email)?> LoginTrabajadorEmailAsync(LoginDto dto)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string hashedPassword = HashPassword(dto.Password);
            
            string query = @"
        SELECT ID_Trabajador, Email
        FROM Trabajadores
        WHERE Email = @Email AND PasswordHash = @Password";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@Password", hashedPassword);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("ID_Trabajador"));
                string email = reader.GetString(reader.GetOrdinal("Email"));

                return (id, email);
            }

            return null;
        }

        // ===================== ADMINISTRADORES =====================

        public async Task<(int Id, string Email)?> LoginAdminEmailAsync(LoginDto dto)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string hashedPassword = HashPassword(dto.Password);
            
            string query = @"
        SELECT id_administrador, email
        FROM administradores
        WHERE email = @email AND passwordhash = @Password";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@email", dto.Email);
            cmd.Parameters.AddWithValue("@Password", hashedPassword);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("id_administrador"));
                string email = reader.GetString(reader.GetOrdinal("email"));

                return (id, email);
            }

            return null;
        }

        // ===================== RECUPERACIÓN PASSWORD =====================

        public async Task<int> ActualizarPasswordPorCorreoAsync(string correo, string nuevaPassword)
        {
            string hashedPassword = HashPassword(nuevaPassword);
            
            string query = @"
        UPDATE Usuarios
        SET PasswordHash = @pw
        WHERE Email = @correo";

            using NpgsqlConnection con = GetConnection();
            using NpgsqlCommand cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.AddWithValue("@pw", hashedPassword);
            cmd.Parameters.AddWithValue("@correo", correo);

            await con.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task GuardarCodigoRecuperacionAsync(string correo, string codigo)
        {
            const string query = @"
                INSERT INTO RecuperacionPassword (Correo, Codigo, Fecha, Usado)
                VALUES (@Correo, @Codigo, NOW(), false)";

            await ExecuteAsync(query,
                new NpgsqlParameter("@Correo", correo),
                new NpgsqlParameter("@Codigo", codigo));
        }

       public async Task<bool> ValidarCodigoAsync(string correo, string codigo)
{
    const string query = @"
        SELECT COUNT(*) FROM RecuperacionPassword
        WHERE Correo = @Correo AND Codigo = @Codigo AND Usado = false";

    using var con = GetConnection();
    using var cmd = new NpgsqlCommand(query, con);
    
    cmd.Parameters.AddWithValue("@Correo", correo);
    cmd.Parameters.AddWithValue("@Codigo", codigo);
    
    await con.OpenAsync();
    
    // 🔥 CORRECCIÓN: Convertir a long primero, luego a int
    var result = await cmd.ExecuteScalarAsync();
    long count = Convert.ToInt64(result);
    
    return count > 0;
}

        public async Task MarcarCodigoUsadoAsync(string correo, string codigo)
        {
            const string query = @"
                UPDATE RecuperacionPassword
                SET Usado = true
                WHERE Correo = @Correo AND Codigo = @Codigo";

            await ExecuteAsync(query,
                new NpgsqlParameter("@Correo", correo),
                new NpgsqlParameter("@Codigo", codigo));
        }

        public async Task ActualizarPasswordUsuarioAsync(string correo, string nuevoPasswordHash)
        {
            string hashedPassword = HashPassword(nuevoPasswordHash);
            
            const string query = @"
                UPDATE Usuarios
                SET PasswordHash = @PasswordHash
                WHERE Email = @Email";

            await ExecuteAsync(query,
                new NpgsqlParameter("@PasswordHash", hashedPassword),
                new NpgsqlParameter("@Email", correo));
        }

        // ===================== HELPERS =====================

        private async Task<Dictionary<string, object>?> EjecutarLoginAsync(
            string query, params NpgsqlParameter[] parameters)
        {
            using var con = GetConnection();
            using var cmd = new NpgsqlCommand(query, con);
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

        public async Task<int> ExecuteAsync(string query, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection con = GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
            {
                cmd.Parameters.AddRange(parameters);
                await con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }
        
        public async Task<List<ModuloINE>> ObtenerModulosINEAsync()
        {
            string query = @"
        SELECT ID_Modulo, Nombre, Direccion, Latitud, Longitud
        FROM ModulosINE";

            var lista = new List<ModuloINE>();

            using NpgsqlConnection con = GetConnection();
            using NpgsqlCommand cmd = new NpgsqlCommand(query, con);

            await con.OpenAsync();
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

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

        // ===================== TRÁMITES INE =====================

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

            using var cmd = new NpgsqlCommand(sql, conn);
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
                ActaNacimiento = rd.IsDBNull(4) ? null : await rd.GetFieldValueAsync<byte[]>(4),
                ComprobanteDomicilio = rd.IsDBNull(5) ? null : await rd.GetFieldValueAsync<byte[]>(5),
                Identificacion = rd.IsDBNull(6) ? null : await rd.GetFieldValueAsync<byte[]>(6),
                CorreoUsuario = rd.IsDBNull(7) ? "" : rd.GetString(7),
                DireccionUsuario = rd.IsDBNull(8) ? "" : rd.GetString(8)
            };
        }

        public async Task<List<TramiteINEItem>> ObtenerMisTramitesINEAsync(int IdUsuario)
        {
            List<TramiteINEItem> lista = new();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
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
            using NpgsqlConnection conn = GetConnection();
            await conn.OpenAsync();

            using NpgsqlTransaction transaction = await conn.BeginTransactionAsync();

            try
            {
                string insertTramite = @"
INSERT INTO Tramites (ID_Usuario, TipoTramite, Estado, FechaCreacion)
VALUES (@ID_Usuario, 'INE', 'Registrado', NOW())
RETURNING ID_Tramite";

                int idTramite;

                using (NpgsqlCommand cmd = new NpgsqlCommand(insertTramite, conn, transaction))
                {
                    cmd.Parameters.Add("@ID_Usuario", NpgsqlDbType.Integer).Value = dto.IdUsuario;
                    idTramite = (int)await cmd.ExecuteScalarAsync();
                }

                string insertINE = @"
INSERT INTO TramiteINE
(ID_Tramite, CURP, ActaNacimiento, ComprobanteDomicilio, Identificacion)
VALUES
(@ID_Tramite, @CURP, @Acta, @Comprobante, @Identificacion)";

                using (NpgsqlCommand cmd = new NpgsqlCommand(insertINE, conn, transaction))
                {
                    cmd.Parameters.Add("@ID_Tramite", NpgsqlDbType.Integer).Value = idTramite;
                    cmd.Parameters.Add("@CURP", NpgsqlDbType.Varchar, 18).Value = dto.CURP;
                    cmd.Parameters.Add("@Acta", NpgsqlDbType.Bytea).Value = dto.ActaNacimiento;
                    cmd.Parameters.Add("@Comprobante", NpgsqlDbType.Bytea).Value = dto.ComprobanteDomicilio;
                    cmd.Parameters.Add("@Identificacion", NpgsqlDbType.Bytea).Value = dto.Identificacion;

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
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

            using var cmd = new NpgsqlCommand(query, conn);
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
    Nombre || ' ' || ApellidoPaterno AS NombreCompleto,
    Email
FROM Usuarios";

            using var cmd = new NpgsqlCommand(q, con);

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

        // ===================== COMPRAVENTA =====================

        public async Task CrearTramiteCompraventaAsync(CrearTramiteCompraventaDto dto)
        {
            using NpgsqlConnection conn = GetConnection();
            await conn.OpenAsync();

            using NpgsqlTransaction tx = await conn.BeginTransactionAsync();

            try
            {
                string qTramite = @"
INSERT INTO Tramites (ID_Usuario, TipoTramite, Estado, FechaCreacion)
VALUES (@ID_Usuario, 'COMPRAVENTA', 'Registrado', NOW())
RETURNING ID_Tramite";

                int idTramite;

                using (NpgsqlCommand cmd = new NpgsqlCommand(qTramite, conn, tx))
                {
                    cmd.Parameters.Add("@ID_Usuario", NpgsqlDbType.Integer)
                        .Value = dto.IdUsuario;

                    idTramite = (int)await cmd.ExecuteScalarAsync();
                }

                byte[] contratoGenerado = ContratoPdfGenerator.GenerarContrato(
                    dto.Vendedor,
                    dto.Comprador,
                    dto.TipoBien,
                    dto.Monto
                );

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

                using (NpgsqlCommand cmd = new NpgsqlCommand(qCompra, conn, tx))
                {
                    cmd.Parameters.Add("@ID_Tramite", NpgsqlDbType.Integer)
                        .Value = idTramite;

                    cmd.Parameters.Add("@TipoBien", NpgsqlDbType.Varchar, 30)
                        .Value = dto.TipoBien ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@Vendedor", NpgsqlDbType.Varchar, 150)
                        .Value = dto.Vendedor ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@Comprador", NpgsqlDbType.Varchar, 150)
                        .Value = dto.Comprador ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@Monto", NpgsqlDbType.Numeric)
                        .Value = dto.Monto;

                    cmd.Parameters.Add("@ContratoPDF", NpgsqlDbType.Bytea)
                        .Value = contratoGenerado ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@IdVendedor", NpgsqlDbType.Bytea)
                        .Value = dto.IdentificacionVendedor ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@IdComprador", NpgsqlDbType.Bytea)
                        .Value = dto.IdentificacionComprador ?? (object)DBNull.Value;

                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ContratoItemDto>> ObtenerMisContratosCompreventaAsync(int idUsuario)
        {
            var lista = new List<ContratoItemDto>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
        SELECT ID_Tramite, TipoTramite, Estado, FechaCreacion
        FROM Tramites
        WHERE ID_Usuario = @id
        AND TipoTramite IN ('COMPRAVENTA')
        ORDER BY FechaCreacion DESC", conn);

            cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = idUsuario;

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

            var cmd = new NpgsqlCommand(@"
        SELECT ID_Tramite, TipoTramite, Estado, FechaCreacion
        FROM Tramites
        WHERE ID_Usuario = @id
        AND TipoTramite IN ('TESTAMENTO')
        ORDER BY FechaCreacion DESC", conn);

            cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = idUsuario;

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

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = idTramite;

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
FROM Tramites t
INNER JOIN TramiteCompraventa c ON t.ID_Tramite = c.ID_Tramite
INNER JOIN Usuarios u ON t.ID_Usuario = u.ID_Usuario
WHERE t.Estado = @estado";

            using NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
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
FROM Tramites t
INNER JOIN TramiteCompraventa c ON t.ID_Tramite = c.ID_Tramite
INNER JOIN Usuarios u ON t.ID_Usuario = u.ID_Usuario
WHERE t.ID_Tramite = @id";

            using NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
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

        public async Task CambiarEstadoAsync(int idTramite, string nuevoEstado)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            string update = @"UPDATE Tramites
                          SET Estado = @estado,
                              FechaActualizacion = NOW()
                          WHERE ID_Tramite = @id";

            using NpgsqlCommand cmd = new NpgsqlCommand(update, conn);
            cmd.Parameters.AddWithValue("@estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@id", idTramite);

            await cmd.ExecuteNonQueryAsync();

            string correoQuery = @"
            SELECT u.Email
            FROM Tramites t
            INNER JOIN Usuarios u ON t.ID_Usuario = u.ID_Usuario
            WHERE t.ID_Tramite = @id";

            using NpgsqlCommand cmdCorreo = new NpgsqlCommand(correoQuery, conn);
            cmdCorreo.Parameters.AddWithValue("@id", idTramite);

            string correo = (string)await cmdCorreo.ExecuteScalarAsync();

            string asunto = "Resultado de tu contrato de compraventa";
            string mensaje = $"Tu contrato fue {nuevoEstado} correctamente.";
            var emailService = new EmailService();

            await emailService.EnviarCorreoAsync(correo, asunto, mensaje);
        }

        // ===================== FUNCIONES ADMIN =====================

        public async Task<List<TrabajadorItemDto>> ObtenerTrabajadoresAsync()
        {
            var lista = new List<TrabajadorItemDto>();

            using var con = GetConnection();

            string q = @"
        SELECT 
            ID_Trabajador,
            Nombre || ' ' || ApellidoPaterno AS NombreCompleto,
            Email
        FROM Trabajadores
        ORDER BY Nombre";

            using var cmd = new NpgsqlCommand(q, con);

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

        public async Task<bool> EliminarUsuarioAsync(int id)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string q1 = @"
        DELETE FROM TramiteINE
        WHERE ID_Tramite IN (
            SELECT ID_Tramite FROM Tramites WHERE ID_Usuario = @id
        )";

            using (var cmd1 = new NpgsqlCommand(q1, con))
            {
                cmd1.Parameters.AddWithValue("@id", id);
                await cmd1.ExecuteNonQueryAsync();
            }

            string q2 = "DELETE FROM Tramites WHERE ID_Usuario = @id";

            using (var cmd2 = new NpgsqlCommand(q2, con))
            {
                cmd2.Parameters.AddWithValue("@id", id);
                await cmd2.ExecuteNonQueryAsync();
            }

            string q3 = "DELETE FROM Usuarios WHERE ID_Usuario = @id";

            using (var cmd3 = new NpgsqlCommand(q3, con))
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

            using var cmd = new NpgsqlCommand(q, con);

            cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = id;

            await con.OpenAsync();

            int filasAfectadas = await cmd.ExecuteNonQueryAsync();

            return filasAfectadas > 0;
        }

        public async Task<bool> TrabajadorExisteAsync(string email)
        {
            string query = @"
        SELECT COUNT(*) FROM Trabajadores WHERE Email = @e";

            using var con = GetConnection();
            using var cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.Add("@e", NpgsqlDbType.Varchar, 150).Value = email;

            await con.OpenAsync();

            int result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return result > 0;
        }

        public async Task<int> InsertarTrabajadorAsync(CrearTrabajadorDto dto)
        {
            string query = @"
        INSERT INTO Trabajadores
        (Nombre, ApellidoPaterno, ApellidoMaterno, Email, Cargo, Departamento, PasswordHash)
        VALUES
        (@n, @ap, @am, @e, @c, @d, @p)
        RETURNING ID_Trabajador";

            using var con = GetConnection();
            using var cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.Add("@n", NpgsqlDbType.Varchar, 100).Value = dto.Nombre;
            cmd.Parameters.Add("@ap", NpgsqlDbType.Varchar, 100).Value = dto.ApellidoPaterno;
            cmd.Parameters.Add("@am", NpgsqlDbType.Varchar, 100).Value = dto.ApellidoMaterno;
            cmd.Parameters.Add("@e", NpgsqlDbType.Varchar, 150).Value = dto.Email;
            cmd.Parameters.Add("@c", NpgsqlDbType.Varchar, 100).Value = dto.Cargo;
            cmd.Parameters.Add("@d", NpgsqlDbType.Varchar, 100).Value = dto.Departamento;

            string hashedPassword = HashPassword(dto.Password);
            cmd.Parameters.Add("@p", NpgsqlDbType.Varchar, 500).Value = hashedPassword;

            await con.OpenAsync();

            int idGenerado = (int)await cmd.ExecuteScalarAsync();

            return idGenerado;
        }
    }
}
