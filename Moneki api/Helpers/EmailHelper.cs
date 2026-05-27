using Resend;

namespace Proyecto_servicio.Helpers
{
    public class EmailService
    {
        private readonly ResendClient _resend;
        private readonly string _fromEmail = "noreply@moneki.com";

        public EmailService()
        {
            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? "re_tu_api_key";
            _resend = new ResendClient(apiKey);
        }

        public async Task EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
        {
            var email = new EmailMessage
            {
                From = _fromEmail,
                To = { correoDestino },
                Subject = asunto,
                HtmlBody = $"<p>{mensaje}</p>",
                TextBody = mensaje
            };

            await _resend.Email.SendAsync(email);
        }
    }
}
