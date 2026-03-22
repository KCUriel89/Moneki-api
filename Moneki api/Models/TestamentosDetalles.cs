namespace Moneki_api.Models
{
    public class TestamentoDetalles
    {
        public int IdTramite { get; set; }
        public string NombreUsuario { get; set; }
        public string Estado { get; set; }
        public string EstadoCivil { get; set; }
        public bool TieneHijos { get; set; }
        public int NumeroHijos { get; set; }
        public string BienesDeclarados { get; set; }
        public string CorreoUsuario { get; set; }
        public byte[] PdfGenerado { get; set; }
    }

}
