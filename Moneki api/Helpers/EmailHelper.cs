using Resend;

namespace Proyecto_servicio.Helpers
{
    public class EmailService
    {
        private readonly IResend _resend;
        private readonly string _fromEmail = "noreply@moneki.com";

        public EmailService()
        {
            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
            _resend = new ResendClient(apiKey);
        }

        public async Task EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
        {
            try
            {
                var email = new EmailMessage
                {
                    From = _fromEmail,
                    To = { correoDestino },
                    Subject = asunto,
                    HtmlBody = $"<p>{mensaje}</p>",
                    TextBody = mensaje
                };

                var result = await _resend.Email.SendAsync(email);
                
                // Para Resend 0.5.1, la respuesta no tiene IsSuccessful
                // Simplemente verificamos si hay excepción
                Console.WriteLine($"✅ Email enviado a {correoDestino}, ID: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error Resend: {ex.Message}");
                throw;
            }
        }

        public void EnviarCorreoEnBackground(string correoDestino, string asunto, string mensaje)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await EnviarCorreoAsync(correoDestino, asunto, mensaje);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Background email error: {ex.Message}");
                }
            });
        }
    }
}
