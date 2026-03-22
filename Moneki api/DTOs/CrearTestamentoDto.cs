namespace Moneki_api.DTOs
{
    public class CrearTestamentoDto
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public string EstadoCivil { get; set; }
        public bool TieneHijos { get; set; }
        public int NumeroHijos { get; set; }
        public string BienesDeclarados { get; set; }
        public DateTime Fecha { get; set; }
    }

}
