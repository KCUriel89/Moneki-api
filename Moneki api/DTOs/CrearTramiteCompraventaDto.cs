namespace Moneki_api.DTOs
{
    public class CrearTramiteCompraventaDto
    {
        public int IdUsuario { get; set; }
        public string TipoBien { get; set; }
        public string Vendedor { get; set; }
        public string Comprador { get; set; }
        public decimal Monto { get; set; }

        public byte[] IdentificacionVendedor { get; set; }
        public byte[] IdentificacionComprador { get; set; }
    }

}
