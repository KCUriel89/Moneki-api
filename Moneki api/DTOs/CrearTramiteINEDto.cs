namespace Moneki_api.DTOs
{
    public class CrearTramiteINEDto
    {
        public int IdUsuario { get; set; }
        public string CURP { get; set; }

        public byte[] ActaNacimiento { get; set; }
        public byte[] ComprobanteDomicilio { get; set; }
        public byte[] Identificacion { get; set; }
    }

}
