#pragma warning disable CS8618
namespace Moneki_api.DTOs
{
    public class ContratoCompletoDto
    {
        public int IdTramite { get; set; }
        public string TipoTramite { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        public DateTime Fecha { get; set; }
        public byte[] ContratoPDF { get; set; }
        public byte[] IdentificacionVendedor { get; set; }
        public byte[] IdentificacionComprador { get; set; }

        // INE
        public string CURP { get; set; }
        public string ActaNacimientoBase64 { get; set; }
        public string ComprobanteDomicilioBase64 { get; set; }
        public string IdentificacionBase64 { get; set; }

        // Compraventa
        public string Vendedor { get; set; }
        public string Comprador { get; set; }
        public string TipoBien { get; set; }
        public decimal? Monto { get; set; }
        public string ContratoPDFBase64 { get; set; }
        public string IdentificacionVendedorBase64 { get; set; }
        public string IdentificacionCompradorBase64 { get; set; }

        // Testamento
        public string EstadoCivil { get; set; }
        public bool? TieneHijos { get; set; }
        public int? NumeroHijos { get; set; }
        public string BienesDeclarados { get; set; }

        // Sucesión
        public string TipoSucesion { get; set; }
        public string NombreFallecido { get; set; }
        public DateTime? FechaDefuncion { get; set; }
        public int? NumeroHerederos { get; set; }
    }

}
