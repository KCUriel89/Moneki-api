namespace Moneki_api.DTOs
{
    public class CompraventaPendienteDto
    {
        public int IdTramite { get; set; }
        public string Cliente { get; set; }
        public string TipoBien { get; set; }
        public decimal Monto { get; set; }
        public string Comprador { get; set; }
        public string Vendedor { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
