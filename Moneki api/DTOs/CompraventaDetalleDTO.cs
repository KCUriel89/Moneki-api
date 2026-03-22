namespace Moneki_api.DTOs
{
    public class CompraventaDetalleDto
    {
        public int ID_Tramite { get; set; }
        public int ID_Compraventa { get; set; }

        public string TipoBien { get; set; }
        public decimal Monto { get; set; }
        public string Comprador { get; set; }
        public string Vendedor { get; set; }

        public byte[] IdentificacionVendedor { get; set; }
        public byte[] IdentificacionComprador { get; set; }
        public byte[] ContratoPDF { get; set; }

        public string CorreoUsuario { get; set; }
    }
}
