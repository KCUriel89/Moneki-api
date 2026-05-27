using Resend;
using Microsoft.Extensions.Options;

namespace Proyecto_servicio.Helpers
{
    public class EmailService
    {
        private readonly IResend _resend;
        private readonly string _fromEmail = "ipncecyt13informatica.pa@gmail.com";

        public EmailService()
        {
            // Configurar Resend manualmente (sin DI)
            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? "tu_api_key_aqui";
            _resend = ResendClient.Create(apiKey);
        }

        public async Task EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
        {
            try
            {
                var message = new EmailMessage();
                message.From = _fromEmail;
                message.To.Add(correoDestino);
                message.Subject = asunto;
                message.HtmlBody = $"<p>{mensaje.Replace("\n", "<br/>")}</p>";
                message.TextBody = mensaje;

                var response = await _resend.EmailSendAsync(message);
                
                Console.WriteLine($"✅ Email enviado a {correoDestino}, ID: {response.Content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error Resend: {ex.Message}");
                // No lanzar excepción para no romper la API
            }
        }

        // Método fire-and-forget (no bloquea la respuesta)
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
