namespace Moneki_api.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class LoginResponse
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;
    }
}
