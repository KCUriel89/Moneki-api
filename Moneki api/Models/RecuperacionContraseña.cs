namespace Moneki_api.Models
{
    public class RecuperacionRequest
    {
        public string Email { get; set; }
    }

    public class CambiarPasswordRequest
    {
        public string Email { get; set; }
        public string Codigo { get; set; }
        public string NuevaPassword { get; set; }
    }

}
