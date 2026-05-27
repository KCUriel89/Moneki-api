using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Moneki_api.DTOs;
using Moneki_api.Models;
using Moneki_api.Services;
using Proyecto_servicio.Helpers;
namespace Moneki_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TramitesController : ControllerBase
    {
        private readonly DatabaseService _service;

        public TramitesController(DatabaseService service)
        {
            _service = service;


        }

        [HttpGet("testamento/revision/{idTramite}")]
        public async Task<ActionResult<TestamentoDetalles>> ObtenerDetalleTestamento(int idTramite)
        {
            var resultado = await _service.ObtenerDetalleTestamentoAsync(idTramite);

            if (resultado == null)
                return NotFound();

            return Ok(resultado);
        }
        [HttpPut("estado")]
        public async Task<IActionResult> ActualizarEstado(ActualizarEstadoDTO dto)
        {
            await _service.ActualizarEstadoTramiteAsync(dto.IdTramite, dto.Estado);
            return Ok();
        }
        [HttpPut("rechazar")]
        public async Task<IActionResult> RechazarTramite(RechazarTramiteDTO dto)
        {
            await _service.RechazarTramiteAsync(dto.IdTramite, dto.Motivo);
            return Ok();
        }

        [HttpGet("Compraventapendientes")]
        public async Task<ActionResult<List<CompraventaDetalleDto>>> ObtenerCompraventaPendientes()
        {
            var lista = await _service.ObtenerPendientesAsync();
            return Ok(lista);
        }

        [HttpGet("Compraventadetalle/{id}")]
        public async Task<ActionResult<CompraventaDetalleDto>> ObtenerCompraventaDetalle(int id)
        {
            var detalle = await _service.ObtenerDetalleAsync(id);
            return Ok(detalle);
        }

        [HttpPost("Compraventacambiar-estado")]
        public async Task<ActionResult> CambiarEstadoCompraventa(int id, string estado)
        {
            await _service.CambiarEstadoAsync(id, estado);
            return Ok();
        }
        [HttpGet("testamentos/revision")]


        public async Task<ActionResult<List<TestamentoRevisionItem>>> ObtenerTestamentosPendientes()
        {
            var lista = await _service.ObtenerTestamentosPendientesAsync();
            return Ok(lista);
        }
        [HttpGet("testamento/{idTramite}/pdf")]
        public async Task<IActionResult> ObtenerPdf(int idTramite)
        {
            var bytes = await _service.ObtenerPdfAsync(idTramite);

            if (bytes == null)
                return NotFound();

            return File(bytes, "application/pdf", $"Testamento_{idTramite}.pdf");
        }

        [HttpGet("usuario/{id}/obtener")]
        public async Task<ActionResult<List<TestamentoListaItem>>> ObtenerPorUsuario(int id)
        {
            var lista = await _service.ObtenerTestamentosUsuarioAsync(id);
            return Ok(lista);
        }
        [HttpPost("testamento")]
        public async Task<IActionResult> CrearTestamento(
     [FromBody] CrearTestamentoDto dto)
        {
            try
            {
                await _service.CrearTramiteTestamentoAsync(dto);
                return Ok(new { mensaje = "Testamento creado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{idTramite}/correo")]
        public async Task<ActionResult<string>> ObtenerCorreo(int idTramite)
        {
            var correo = await _service.ObtenerCorreoUsuarioPorTramiteAsync(idTramite);

            if (correo == null)
                return NotFound();

            return Ok(correo);
        }
        [HttpPut("ine/{idTramite}/aceptar")]
        public async Task<IActionResult> AceptarINE(int idTramite)
        {
            // 🔹 Actualizar estado
            await _service.ActualizarEstadoTramiteINEAsync(idTramite, "Aceptado");

            // 🔹 Obtener correo del usuario
            string? correo = await _service.ObtenerCorreoUsuarioPorTramiteINEAsync(idTramite);

            if (!string.IsNullOrEmpty(correo))
            {
                var emailService = new EmailService();

                await emailService.EnviarCorreoAsync(
                    correo,
                    "INE aprobada",
                    "Tu trámite de INE ha sido aprobado correctamente."
                );
            }

            return Ok("Trámite INE aceptado.");
        }

        [HttpPut("ine/{idTramite}/rechazar")]
        public async Task<IActionResult> RechazarINE(int idTramite, [FromBody] string motivo)
        {
            await _service.RechazarTramiteAsync(idTramite, motivo);

            string? correo = await _service.ObtenerCorreoUsuarioPorTramiteINEAsync(idTramite);

            if (!string.IsNullOrEmpty(correo))
            {
                var emailService = new EmailService();

                await emailService.EnviarCorreoAsync(
                    correo,
                    "INE rechazada",
                    $"Tu trámite fue rechazado.\n\nMotivo:\n{motivo}"
                );
            }

            return Ok("Trámite rechazado.");
        }
        [HttpPost("modulo-cercano")]
        public async Task<IActionResult> ObtenerModuloCercano([FromBody] ModuloCercanoDto dto)
        {
            var resultado = await _service.ObtenerModuloMasCercanoAsync(dto);
            return Ok(resultado);
        }
        [HttpGet("correo-usuario/{idTramite}")]
        public async Task<IActionResult> ObtenerCorreoUsuario(int idTramite)
        {
            var correo = await _service.ObtenerCorreoUsuarioPorTramiteAsync(idTramite);
            return Ok(correo);
        }
        [HttpGet("ine/pendientes")]
        public async Task<IActionResult> ObtenerTramitesINEPendientes()
        {
            var lista = await _service.ObtenerTramitesINEPendientesAsync();
            return Ok(lista);
        }
        [HttpPut("estado/tramite")]
        public async Task<IActionResult> ActualizarEstadoTramite([FromBody] ActualizarEstadoTramiteDto dto)
        {
            await _service.ActualizarEstadoTramite(
                dto.IdTramite,
                dto.Estado,
                dto.Observaciones);

            return Ok();
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            await _service.RegisterUserAsync(
                dto.Nombre,
                dto.ApellidoPaterno,
                dto.ApellidoMaterno,
                dto.Email,
                dto.Password,
                dto.Telefono,
                dto.Direccion,
                dto.FechaNacimiento,
                DateTime.Now,
                dto.Latitud,
                dto.Longitud
            );

            return Ok();
        }
        [HttpPost("exists")]
        public async Task<IActionResult> UserExists([FromBody] EmailDto dto)
        {
            var exists = await _service.UserExistsAsync(dto.Email);
            return Ok(exists);
        }


        [HttpPost("correo-existe")]
        public async Task<IActionResult> CorreoExiste([FromBody] CorreoDto dto)
        {
            var existe = await _service.CorreoExisteUsuariosAsync(dto.Correo);
            return Ok(existe);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            int filas = await _service.ActualizarPasswordPorCorreoAsync(
                dto.Correo,
                dto.NuevaPassword
            );

            if (filas == 0)
                return NotFound("Usuario no encontrado");

            return Ok(true);
        }
        [HttpPost("guardar-codigo-recuperacion")]
        public async Task<IActionResult> GuardarCodigoRecuperacion(
    [FromBody] CodigoRecuperacionDto dto)
        {
            await _service.GuardarCodigoRecuperacionAsync(
                dto.Correo,
                dto.Codigo
            );

            return Ok();
        }
        [HttpPost("validar-codigo")]
        public async Task<IActionResult> ValidarCodigo([FromBody] ValidarCodigoDto dto)
        {
            bool valido = await _service.ValidarCodigoAsync(
                dto.Correo,
                dto.Codigo
            );

            return Ok(valido);
        }
        [HttpPost("marcar-codigo-usado")]
        public async Task<IActionResult> MarcarCodigoUsado(
    [FromBody] MarcarCodigoUsadoDto dto)
        {
            await _service.MarcarCodigoUsadoAsync(
                dto.Correo,
                dto.Codigo
            );

            return Ok();
        }
        [HttpPost("actualizar-password")]
        public async Task<IActionResult> ActualizarPassword(
    [FromBody] ActualizarPasswordDto dto)
        {
            await _service.ActualizarPasswordUsuarioAsync(
                dto.Correo,
                dto.NuevoPassword
            );

            return Ok();
        }
        [HttpGet("modulos/ine")]
        public async Task<IActionResult> ObtenerModulosINE()
        {
            var modulos = await _service.ObtenerModulosINEAsync();
            return Ok(modulos);
        }
        [HttpGet("ine/{idTramite}")]
        public async Task<ActionResult<INECompleto>> ObtenerINECompleto(int idTramite)
        {
            var resultado = await _service.ObtenerINECompleto(idTramite);

            if (resultado == null)
                return NotFound();

            return Ok(resultado);
        }
        [HttpGet("ine/usuario/{idUsuario}")]
        public async Task<IActionResult> ObtenerMisTramitesINE(int idUsuario)
        {
            var lista = await _service.ObtenerMisTramitesINEAsync(idUsuario);
            return Ok(lista);
        }
        [HttpPost("ine/crear-tramite")]
        public async Task<IActionResult> CrearTramiteINE([FromBody] CrearTramiteINEDto dto)
        {
            await _service.CrearTramiteINEAsync(dto);
            return Ok();
        }
        [HttpGet("usuario/{idUsuario}")]
        public async Task<List<TramiteModel>> GetTramitesUsuario(int idUsuario)
        {
            return await _service.GetTramitesUsuarioAsync(idUsuario);
        }
        [HttpGet("usuarios")]
        public async Task<List<UsuarioItem>> ObtenerUsuarios()
        {
            return await _service.ObtenerUsuariosAsync();
        }
        [HttpPost("tramites/compraventa")]
        public async Task<IActionResult> CrearTramiteCompraventa([FromBody] CrearTramiteCompraventaDto dto)
        {
            await _service.CrearTramiteCompraventaAsync(dto);
            return Ok();
        }
        [HttpGet("mis-contratos/{idUsuario}")]
        public async Task<List<ContratoItemDto>> ObtenerMisContratos(int idUsuario)
        {
            return await _service.ObtenerMisContratosCompreventaAsync(idUsuario);
        }
        [HttpGet("mis-testamentos/{idUsuario}")]
        public async Task<List<ContratoItemDto>> ObtenerMisTestamentos(int idUsuario)
        {
            return await _service.ObtenerMisTestamentosAsync(idUsuario);
        }
        [HttpGet("contrato-completo/{id}")]
        public async Task<ContratoCompletoDto> ObtenerContratoCompleto(int id)
        {
            return await _service.ObtenerContratoCompletoAsync(id);
        }
        [HttpGet("trabajadores")]
        public async Task<List<TrabajadorItemDto>> ObtenerTrabajadores()
        {
            return await _service.ObtenerTrabajadoresAsync();
        }
        [HttpDelete("usuarios/{id}")]
        public async Task<bool> EliminarUsuario(int id)
        {
            try
            {
                return await _service.EliminarUsuarioAsync(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        [HttpDelete("trabajadores/{id}")]
        public async Task<bool> EliminarTrabajador(int id)
        {
            return await _service.EliminarTrabajadorAsync(id);
        }
        [HttpGet("trabajadores/existe/{email}")]
        public async Task<bool> TrabajadorExiste(string email)
        {
            return await _service.TrabajadorExisteAsync(email);
        }
        [HttpPost("trabajadores")]
        public async Task<int> InsertarTrabajador([FromBody] CrearTrabajadorDto dto)
        {
            return await _service.InsertarTrabajadorAsync(dto);
        }
        [HttpPost("recuperar")]
public async Task<IActionResult> RecuperarPassword([FromBody] RecuperacionRequest request)
{
    try
    {
        var existe = await _service.CorreoExisteUsuariosAsync(request.Email);

        if (!existe)
            return NotFound("Correo no registrado");

        string codigo = new Random().Next(100000, 999999).ToString();

        await _service.GuardarCodigoRecuperacionAsync(request.Email, codigo);
        
        // Enviar email en segundo plano (no espera)
        var emailService = new EmailService();
        _ = Task.Run(() => emailService.EnviarCorreoAsync(
            request.Email,
            "Recuperación de contraseña",
            $"Tu código es: {codigo}"
        ).ContinueWith(t => 
        {
            if (t.IsFaulted)
                Console.WriteLine($"Error enviando email: {t.Exception?.Message}");
        }));

        // Responder inmediatamente
        return Ok(new { mensaje = "Si el correo está registrado, recibirás un código", email = request.Email });
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Error: {ex.Message}");
    }
}
        [HttpPost("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
        {
            bool valido = await _service.ValidarCodigoAsync(request.Email, request.Codigo);

            if (!valido)
                return BadRequest("Código inválido");

            await _service.MarcarCodigoUsadoAsync(request.Email, request.Codigo);

            await _service.ActualizarPasswordUsuarioAsync(request.Email, request.NuevaPassword);

            return Ok();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _service.LoginUsuarioEmailAsync(dto);

            if (user != null)
                return Ok(new
                {
                    Id = user.Value.Id,
                    Email = user.Value.Email,
                    Rol = "Usuario"
                });

            var admin = await _service.LoginAdminEmailAsync(dto);

            if (admin != null)
                return Ok(new
                {
                    Id = admin.Value.Id,
                    Email = admin.Value.Email,
                    Rol = "Admin"
                });

            var trabajador = await _service.LoginTrabajadorEmailAsync(dto);

            if (trabajador != null)
                return Ok(new
                {
                    Id = trabajador.Value.Id,
                    Email = trabajador.Value.Email,
                    Rol = "Trabajador"
                });

            return Unauthorized();
        }

        [HttpPut("testamento/{idTramite}/aceptar")]
        public async Task<IActionResult> AceptarTestamento(int idTramite)
        {
            await _service.ActualizarEstadoTramiteAsync(idTramite, "Aceptado");

            string? correo = await _service.ObtenerCorreoUsuarioPorTramiteAsync(idTramite);

            if (!string.IsNullOrEmpty(correo))
            {
                var emailService = new EmailService();

                await emailService.EnviarCorreoAsync(
                    correo,
                    "Testamento aprobado",
                    "Tu trámite ha sido aprobado correctamente."
                );
            }

            return Ok("Trámite aceptado.");
        }

        [HttpPut("testamento/{idTramite}/rechazar")]
        public async Task<IActionResult> RechazarTestamento(int idTramite)
        {
            await _service.ActualizarEstadoTramiteAsync(idTramite, "Rechazado");

            string? correo = await _service.ObtenerCorreoUsuarioPorTramiteAsync(idTramite);

            if (!string.IsNullOrEmpty(correo))
            {
                var emailService = new EmailService();

                await emailService.EnviarCorreoAsync(
                    correo,
                    "Testamento rechazado",
                    "Tu trámite fue rechazado. Revisa la plataforma para más información."
                );
            }

            return Ok("Trámite rechazado.");
        }

    }
}

