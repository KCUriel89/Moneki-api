namespace Moneki_api.DTOs
{
    public class RechazarTramiteDTO
    {
        public int IdTramite { get; set; }
        public string Motivo { get; set; }
    }
    public class RechazoRequest
    {
        public string Motivo { get; set; }
    }
}
