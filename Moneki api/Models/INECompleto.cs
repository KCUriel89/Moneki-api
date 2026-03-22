namespace Moneki_api.Models
{
    public class INECompleto
    {
        public int IdTramite { get; set; }
        public string CURP { get; set; }
        public string Estado { get; set; }
        public DateTime Fecha { get; set; }

        public byte[]? ActaNacimiento { get; set; }
        public byte[]? ComprobanteDomicilio { get; set; }
        public byte[]? Identificacion { get; set; }

        public string CorreoUsuario { get; set; }
        public string DireccionUsuario { get; set; }
    }

}
