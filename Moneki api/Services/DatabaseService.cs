using Moneki_api.DTOs;
using Moneki_api.Helpers;
using Moneki_api.Models;
using Proyecto_servicio.Helpers;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace Moneki_api.Services
{
    public class DatabaseService
    {
        private readonly Client _supabase;

        public DatabaseService(Client supabase)
        {
            _supabase = supabase;
        }

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
            // Obtener el trámite con sus relaciones
            var tramiteResult = await _supabase
                .From<TramiteSupabase>()
                .Select("*, TramiteTestamento!inner(*), Usuarios!inner(Nombre, ApellidoPaterno, ApellidoMaterno)")
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();

            var tramite = tramiteResult.Models.FirstOrDefault();
            if (tramite == null) return null;

            // Obtener el testamento relacionado (esto requiere que tu modelo tenga las propiedades de navegación)
            // Como alternativa, hacemos una consulta separada
            var testamentoResult = await _supabase
                .From<TramiteTestamentoSupabase>()
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();

            var testamento = testamentoResult.Models.FirstOrDefault();
            if (testamento == null) return null;

            var usuarioResult = await _supabase
                .From<UsuarioSupabase>()
                .Select("Nombre, ApellidoPaterno, ApellidoMaterno")
                .Filter("ID_Usuario", Operator.Equals, tramite.ID_Usuario)
                .Get();

            var usuario = usuarioResult.Models.FirstOrDefault();

            return new TestamentoDetalles
            {
                Estado = tramite.Estado,
                EstadoCivil = testamento.EstadoCivil,
                TieneHijos = testamento.TieneHijos,
                NumeroHijos = testamento.NumeroHijos,
                BienesDeclarados = testamento.BienesDeclarados,
                PdfGenerado = testamento.Pdf,
                NombreUsuario = usuario != null ? $"{usuario.Nombre} {usuario.ApellidoPaterno} {usuario.ApellidoMaterno}" : ""
            };
        }

        public async Task ActualizarEstadoTramiteINEAsync(int idTramite, string estado)
        {
            await _supabase
                .From<TramiteSupabase>()
                .Where(x => x.ID_Tramite == idTramite)
                .Set(x => x.Estado, estado)
                .Set(x => x.FechaActualizacion, DateTime.UtcNow)
                .Update();
        }

        public async Task<string?> ObtenerCorreoUsuarioPorTramiteINEAsync(int idTramite)
        {
            var tramiteResult = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Usuario")
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();

            var tramite = tramiteResult.Models.FirstOrDefault();
            if (tramite == null) return null;

            var usuarioResult = await _supabase
                .From<UsuarioSupabase>()
                .Select("Email")
                .Filter("ID_Usuario", Operator.Equals, tramite.ID_Usuario)
                .Get();

            return usuarioResult.Models.FirstOrDefault()?.Email;
        }

        public async Task ActualizarEstadoTramiteAsync(int idTramite, string estado)
        {
            await _supabase
                .From<TramiteSupabase>()
                .Where(x => x.ID_Tramite == idTramite)
                .Set(x => x.Estado, estado)
                .Set(x => x.FechaActualizacion, DateTime.UtcNow)
                .Update();
        }

        public async Task RechazarTramiteAsync(int idTramite, string motivo)
        {
            await _supabase
                .From<TramiteSupabase>()
                .Where(x => x.ID_Tramite == idTramite)
                .Set(x => x.Estado, "Rechazado")
                .Set(x => x.Observaciones, motivo)
                .Set(x => x.FechaActualizacion, DateTime.UtcNow)
                .Update();
        }

        public async Task<List<TestamentoListaItem>> ObtenerTestamentosParaRevisionAsync()
        {
            var result = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Tramite, Estado, TramiteTestamento!inner(EstadoCivil)")
                .Filter("TipoTramite", Operator.Equals, "TESTAMENTO")
                .Filter("Estado", Operator.In, new[] { "Registrado", "En revisión" })
                .Get();

            var lista = new List<TestamentoListaItem>();
            foreach (var tramite in result.Models)
            {
                // Obtener el testamento relacionado
                var testamentoResult = await _supabase
                    .From<TramiteTestamentoSupabase>()
                    .Select("EstadoCivil")
                    .Filter("ID_Tramite", Operator.Equals, tramite.ID_Tramite)
                    .Get();
                    
                var testamento = testamentoResult.Models.FirstOrDefault();
                
                lista.Add(new TestamentoListaItem
                {
                    IdTramite = tramite.ID_Tramite,
                    Estado = tramite.Estado,
                    EstadoCivil = testamento?.EstadoCivil ?? ""
                });
            }
            return lista;
        }

        public async Task<List<TestamentoRevisionItem>> ObtenerTestamentosPendientesAsync()
        {
            var result = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Tramite, Estado, FechaCreacion")
                .Filter("TipoTramite", Operator.Equals, "TESTAMENTO")
                .Filter("Estado", Operator.Equals, "Registrado")
                .Order("FechaCreacion", Ordering.Descending)
                .Get();

            var lista = new List<TestamentoRevisionItem>();
            foreach (var tramite in result.Models)
            {
                // Obtener usuario
                var usuarioResult = await _supabase
                    .From<UsuarioSupabase>()
                    .Select("Nombre, ApellidoPaterno, ApellidoMaterno")
                    .Filter("ID_Usuario", Operator.Equals, tramite.ID_Usuario)
                    .Get();
                var usuario = usuarioResult.Models.FirstOrDefault();

                // Obtener testamento
                var testamentoResult = await _supabase
                    .From<TramiteTestamentoSupabase>()
                    .Select("EstadoCivil, TieneHijos, NumeroHijos")
                    .Filter("ID_Tramite", Operator.Equals, tramite.ID_Tramite)
                    .Get();
                var testamento = testamentoResult.Models.FirstOrDefault();

                lista.Add(new TestamentoRevisionItem
                {
                    IdTramite = tramite.ID_Tramite,
                    NombreUsuario = usuario != null ? $"{usuario.Nombre} {usuario.ApellidoPaterno} {usuario.ApellidoMaterno}" : "",
                    EstadoCivil = testamento?.EstadoCivil ?? "",
                    TieneHijos = testamento?.TieneHijos ?? false,
                    NumeroHijos = testamento?.NumeroHijos ?? 0,
                    Estado = tramite.Estado,
                    Fecha = tramite.FechaCreacion
                });
            }
            return lista;
        }

        public async Task<byte[]?> ObtenerPdfAsync(int idTramite)
        {
            var result = await _supabase
                .From<TramiteTestamentoSupabase>()
                .Select("Pdf")
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();

            return result.Models.FirstOrDefault()?.Pdf;
        }

        public async Task<List<TestamentoListaItem>> ObtenerTestamentosUsuarioAsync(int idUsuario)
        {
            var result = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Tramite, Estado")
                .Filter("ID_Usuario", Operator.Equals, idUsuario)
                .Filter("TipoTramite", Operator.Equals, "TESTAMENTO")
                .Get();

            var lista = new List<TestamentoListaItem>();
            foreach (var tramite in result.Models)
            {
                var testamentoResult = await _supabase
                    .From<TramiteTestamentoSupabase>()
                    .Select("EstadoCivil")
                    .Filter("ID_Tramite", Operator.Equals, tramite.ID_Tramite)
                    .Get();
                var testamento = testamentoResult.Models.FirstOrDefault();

                lista.Add(new TestamentoListaItem
                {
                    IdTramite = tramite.ID_Tramite,
                    Estado = tramite.Estado,
                    EstadoCivil = testamento?.EstadoCivil ?? ""
                });
            }
            return lista;
        }

        public async Task CrearTramiteTestamentoAsync(CrearTestamentoDto dto)
        {
            // 1. Insertar trámite
            var newTramite = new TramiteSupabase
            {
                ID_Usuario = dto.IdUsuario,
                TipoTramite = "TESTAMENTO",
                Estado = "Registrado",
                FechaCreacion = DateTime.UtcNow
            };
            
            var insertedTramite = await _supabase.From<TramiteSupabase>().Insert(newTramite);
            int idTramite = insertedTramite.Models.First().ID_Tramite;

            // 2. Generar PDF
            byte[] pdf = TestamentoPdfGenerator.GenerarTestamento(
                dto.NombreCompleto,
                dto.EstadoCivil,
                dto.TieneHijos,
                dto.NumeroHijos,
                dto.BienesDeclarados,
                dto.Fecha
            );

            // 3. Insertar testamento
            var newTestamento = new TramiteTestamentoSupabase
            {
                ID_Tramite = idTramite,
                EstadoCivil = dto.EstadoCivil,
                TieneHijos = dto.TieneHijos,
                NumeroHijos = dto.TieneHijos ? dto.NumeroHijos : 0,
                BienesDeclarados = dto.BienesDeclarados,
                Pdf = pdf,
                DeclaracionAceptada = true
            };
            
            await _supabase.From<TramiteTestamentoSupabase>().Insert(newTestamento);
        }

        public async Task<string?> ObtenerCorreoUsuarioPorTramiteAsync(int idTramite)
        {
            var tramiteResult = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Usuario")
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();

            var tramite = tramiteResult.Models.FirstOrDefault();
            if (tramite == null) return null;

            var usuarioResult = await _supabase
                .From<UsuarioSupabase>()
                .Select("Email")
                .Filter("ID_Usuario", Operator.Equals, tramite.ID_Usuario)
                .Get();

            return usuarioResult.Models.FirstOrDefault()?.Email;
        }

        // ===================== INE =====================

        public async Task AceptarINEAsync(AceptarIneDto dto)
        {
            // 1. Actualizar trámite
            await _supabase
                .From<TramiteSupabase>()
                .Where(x => x.ID_Tramite == dto.IdTramite)
                .Set(x => x.Estado, "Aceptado")
                .Set(x => x.ID_Trabajador, dto.IdTrabajador)
                .Set(x => x.FechaActualizacion, DateTime.UtcNow)
                .Update();

            // 2. Obtener módulo más cercano
            string modulo = await ObtenerModuloMasCercanoAsync(new ModuloCercanoDto { DireccionUsuario = dto.DireccionUsuario });

            // 3. Enviar correo
            var emailService = new EmailService();
            await emailService.EnviarCorreoAsync(
                dto.CorreoUsuario,
                "Trámite INE Aceptado",
                $"Tu trámite fue ACEPTADO.\n\nAcude al módulo:\n{modulo}"
            );
        }

        public async Task RechazarINEAsync(RechazarIneDto dto)
        {
            await _supabase
                .From<TramiteSupabase>()
                .Where(x => x.ID_Tramite == dto.IdTramite)
                .Set(x => x.Estado, "Rechazado")
                .Set(x => x.ID_Trabajador, dto.IdTrabajador)
                .Set(x => x.Observaciones, dto.Motivo)
                .Set(x => x.FechaActualizacion, DateTime.UtcNow)
                .Update();

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
            var result = await _supabase
                .From<ModuloINESupabase>()
                .Select("Nombre, Direccion")
                .Limit(1)
                .Get();

            if (result.Models.Any())
            {
                var modulo = result.Models.First();
                return $"{modulo.Nombre}\n{modulo.Direccion}";
            }

            return "No se encontró un módulo INE disponible";
        }

        public async Task<string> ObtenerCorreoUsuarioPorTramite(int idTramite)
        {
            var tramiteResult = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Usuario")
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();

            var tramite = tramiteResult.Models.FirstOrDefault();
            if (tramite == null) return "";

            var usuarioResult = await _supabase
                .From<UsuarioSupabase>()
                .Select("Email")
                .Filter("ID_Usuario", Operator.Equals, tramite.ID_Usuario)
                .Get();

            return usuarioResult.Models.FirstOrDefault()?.Email ?? "";
        }

        public async Task<List<TramiteINEItem>> ObtenerTramitesINEPendientesAsync()
        {
            var result = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Tramite, Estado, FechaCreacion")
                .Filter("Estado", Operator.Equals, "Registrado")
                .Filter("TipoTramite", Operator.Equals, "INE")
                .Order("FechaCreacion", Ordering.Descending)
                .Get();

            var lista = new List<TramiteINEItem>();
            foreach (var tramite in result.Models)
            {
                var ineResult = await _supabase
                    .From<TramiteINESupabase>()
                    .Select("CURP")
                    .Filter("ID_Tramite", Operator.Equals, tramite.ID_Tramite)
                    .Get();
                var ine = ineResult.Models.FirstOrDefault();

                lista.Add(new TramiteINEItem
                {
                    IdTramite = tramite.ID_Tramite,
                    Estado = tramite.Estado,
                    FechaCreacion = tramite.FechaCreacion,
                    CURP = ine?.CURP ?? ""
                });
            }
            return lista;
        }

        public async Task ActualizarEstadoTramite(int idTramite, string estado, string observaciones = null)
        {
            var update = _supabase
                .From<TramiteSupabase>()
                .Where(x => x.ID_Tramite == idTramite)
                .Set(x => x.Estado, estado)
                .Set(x => x.FechaActualizacion, DateTime.UtcNow);
                
            if (observaciones != null)
            {
                update.Set(x => x.Observaciones, observaciones);
            }
            
            await update.Update();
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
            var newUser = new UsuarioSupabase
            {
                Nombre = nombre,
                ApellidoPaterno = apellidoPaterno,
                ApellidoMaterno = apellidoMaterno,
                Email = email,
                PasswordHash = HashPassword(password), // ¡Usando hash!
                Telefono = telefono,
                Direccion = direccion,
                FechaNacimiento = fechaNacimiento,
                FechaRegistro = fechaRegistro,
                Latitud = latitud,
                Longitud = longitud
            };

            await _supabase.From<UsuarioSupabase>().Insert(newUser);
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var result = await _supabase
                .From<UsuarioSupabase>()
                .Select("ID_Usuario")
                .Filter("Email", Operator.Equals, email)
                .Get();

            return result.Models.Any();
        }

        public async Task<(int Id, string Email)?> LoginUsuarioEmailAsync(LoginDto dto)
        {
            var hashedPassword = HashPassword(dto.Password);
            
            var result = await _supabase
                .From<UsuarioSupabase>()
                .Select("ID_Usuario, Email")
                .Filter("Email", Operator.Equals, dto.Email)
                .Filter("PasswordHash", Operator.Equals, hashedPassword)
                .Get();

            var user = result.Models.FirstOrDefault();
            if (user != null)
                return (user.ID_Usuario, user.Email);

            return null;
        }

        public async Task<bool> CorreoExisteUsuariosAsync(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return false;

            correo = correo.Trim().ToLower();

            var result = await _supabase
                .From<UsuarioSupabase>()
                .Select("Email")
                .Filter("Email", Operator.Equals, correo)
                .Get();

            return result.Models.Any();
        }

        // ===================== TRABAJADORES =====================

        public async Task<(int Id, string Email)?> LoginTrabajadorEmailAsync(LoginDto dto)
        {
            var hashedPassword = HashPassword(dto.Password);
            
            var result = await _supabase
                .From<TrabajadorSupabase>()
                .Select("ID_Trabajador, Email")
                .Filter("Email", Operator.Equals, dto.Email)
                .Filter("PasswordHash", Operator.Equals, hashedPassword)
                .Get();

            var trabajador = result.Models.FirstOrDefault();
            if (trabajador != null)
                return (trabajador.ID_Trabajador, trabajador.Email);

            return null;
        }

        // ===================== ADMINISTRADORES =====================

        public async Task<(int Id, string Email)?> LoginAdminEmailAsync(LoginDto dto)
        {
            var hashedPassword = HashPassword(dto.Password);
            
            var result = await _supabase
                .From<AdministradorSupabase>()
                .Select("ID_Administrador, Email")
                .Filter("Email", Operator.Equals, dto.Email)
                .Filter("PasswordHash", Operator.Equals, hashedPassword)
                .Get();

            var admin = result.Models.FirstOrDefault();
            if (admin != null)
                return (admin.ID_Administrador, admin.Email);

            return null;
        }

        // ===================== RECUPERACIÓN PASSWORD =====================

        public async Task<int> ActualizarPasswordPorCorreoAsync(string correo, string nuevaPassword)
        {
            var hashedPassword = HashPassword(nuevaPassword);
            
            var result = await _supabase
                .From<UsuarioSupabase>()
                .Where(x => x.Email == correo)
                .Set(x => x.PasswordHash, hashedPassword)
                .Update();

            return result.Models.Count;
        }

        public async Task GuardarCodigoRecuperacionAsync(string correo, string codigo)
        {
            var newRecord = new RecuperacionPasswordSupabase
            {
                Correo = correo,
                Codigo = codigo,
                Fecha = DateTime.UtcNow,
                Usado = false
            };

            await _supabase.From<RecuperacionPasswordSupabase>().Insert(newRecord);
        }

        public async Task<bool> ValidarCodigoAsync(string correo, string codigo)
        {
            var result = await _supabase
                .From<RecuperacionPasswordSupabase>()
                .Select("ID")
                .Filter("Correo", Operator.Equals, correo)
                .Filter("Codigo", Operator.Equals, codigo)
                .Filter("Usado", Operator.Equals, false)
                .Get();

            return result.Models.Any();
        }

        public async Task MarcarCodigoUsadoAsync(string correo, string codigo)
        {
            await _supabase
                .From<RecuperacionPasswordSupabase>()
                .Where(x => x.Correo == correo && x.Codigo == codigo)
                .Set(x => x.Usado, true)
                .Update();
        }

        public async Task ActualizarPasswordUsuarioAsync(string correo, string nuevoPasswordHash)
        {
            var hashedPassword = HashPassword(nuevoPasswordHash);
            
            await _supabase
                .From<UsuarioSupabase>()
                .Where(x => x.Email == correo)
                .Set(x => x.PasswordHash, hashedPassword)
                .Update();
        }

        // ===================== MÓDULOS INE =====================

        public async Task<List<ModuloINE>> ObtenerModulosINEAsync()
        {
            var result = await _supabase
                .From<ModuloINESupabase>()
                .Select("*")
                .Get();

            var lista = new List<ModuloINE>();
            foreach (var modulo in result.Models)
            {
                lista.Add(new ModuloINE
                {
                    IdModulo = modulo.ID_Modulo,
                    Nombre = modulo.Nombre,
                    Direccion = modulo.Direccion,
                    Latitud = modulo.Latitud,
                    Longitud = modulo.Longitud,
                    DistanciaKm = 0
                });
            }

            return lista;
        }

        // ===================== TRÁMITES INE =====================

        public async Task<INECompleto?> ObtenerINECompleto(int idTramite)
        {
            var tramiteResult = await _supabase
                .From<TramiteSupabase>()
                .Select("*")
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();

            var tramite = tramiteResult.Models.FirstOrDefault();
            if (tramite == null) return null;

            var ineResult = await _supabase
                .From<TramiteINESupabase>()
                .Select("*")
                .Filter("ID_Tramite", Operator.Equals, idTramite)
                .Get();
            var ine = ineResult.Models.FirstOrDefault();

            var usuarioResult = await _supabase
                .From<UsuarioSupabase>()
                .Select("Email, Direccion")
                .Filter("ID_Usuario", Operator.Equals, tramite.ID_Usuario)
                .Get();
            var usuario = usuarioResult.Models.FirstOrDefault();

            return new INECompleto
            {
                IdTramite = tramite.ID_Tramite,
                CURP = ine?.CURP ?? "",
                Estado = tramite.Estado,
                Fecha = tramite.FechaCreacion,
                ActaNacimiento = ine?.ActaNacimiento,
                ComprobanteDomicilio = ine?.ComprobanteDomicilio,
                Identificacion = ine?.Identificacion,
                CorreoUsuario = usuario?.Email ?? "",
                DireccionUsuario = usuario?.Direccion ?? ""
            };
        }

        public async Task<List<TramiteINEItem>> ObtenerMisTramitesINEAsync(int IdUsuario)
        {
            var tramitesResult = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Tramite, Estado, FechaCreacion")
                .Filter("ID_Usuario", Operator.Equals, IdUsuario)
                .Filter("TipoTramite", Operator.Equals, "INE")
                .Get();

            var lista = new List<TramiteINEItem>();
            foreach (var tramite in tramitesResult.Models)
            {
                var ineResult = await _supabase
                    .From<TramiteINESupabase>()
                    .Select("CURP")
                    .Filter("ID_Tramite", Operator.Equals, tramite.ID_Tramite)
                    .Get();
                var ine = ineResult.Models.FirstOrDefault();

                lista.Add(new TramiteINEItem
                {
                    IdTramite = tramite.ID_Tramite,
                    CURP = ine?.CURP ?? "",
                    Estado = tramite.Estado,
                    FechaCreacion = tramite.FechaCreacion
                });
            }
            return lista;
        }

        public async Task CrearTramiteINEAsync(CrearTramiteINEDto dto)
        {
            // 1. Insertar trámite
            var newTramite = new TramiteSupabase
            {
                ID_Usuario = dto.IdUsuario,
                TipoTramite = "INE",
                Estado = "Registrado",
                FechaCreacion = DateTime.UtcNow
            };
            
            var insertedTramite = await _supabase.From<TramiteSupabase>().Insert(newTramite);
            int idTramite = insertedTramite.Models.First().ID_Tramite;

            // 2. Insertar datos INE
            var newINE = new TramiteINESupabase
            {
                ID_Tramite = idTramite,
                CURP = dto.CURP,
                ActaNacimiento = dto.ActaNacimiento,
                ComprobanteDomicilio = dto.ComprobanteDomicilio,
                Identificacion = dto.Identificacion
            };
            
            await _supabase.From<TramiteINESupabase>().Insert(newINE);
        }

        // ===================== TRÁMITES GENERALES =====================

        public async Task<List<TramiteModel>> GetTramitesUsuarioAsync(int idUsuario)
        {
            var result = await _supabase
                .From<TramiteSupabase>()
                .Select("ID_Tramite, TipoTramite, Estado, FechaCreacion")
                .Filter("ID_Usuario", Operator.Equals, idUsuario)
                .Order("FechaCreacion", Ordering.Descending)
                .Get();

            var lista = new List<TramiteModel>();
            foreach (var tramite in result.Models)
            {
                lista.Add(new TramiteModel
                {
                    ID_Tramite = tramite.ID_Tramite,
                    TipoTramite = tramite.TipoTramite,
                    Estado = tramite.Estado,
                    FechaCreacion = tramite.FechaCreacion
                });
            }
            return lista;
        }

        public async Task<List<UsuarioItem>> ObtenerUsuariosAsync()
        {
            var result = await _supabase
                .From<UsuarioSupabase>()
                .Select("ID_Usuario, Nombre, ApellidoPaterno, Email")
                .Get();

            var lista = new List<UsuarioItem>();
            foreach (var usuario in result.Models)
            {
                lista.Add(new UsuarioItem
                {
                    ID_Usuario = usuario.ID_Usuario,
                    NombreCompleto = $"{usuario.Nombre} {usuario.ApellidoPaterno}",
                    Email = usuario.Email
                });
            }
            return
