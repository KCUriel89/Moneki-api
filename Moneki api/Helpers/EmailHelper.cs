using Resend;
using Microsoft.Extensions.Options;

namespace Proyecto_servicio.Helpers
{
    public class EmailService
    {
        private readonly IResend _resend;
        private readonly string _fromEmail = "noreply@moneki.com";

        public EmailService()
        {
            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") 
                         ?? "re_tu_api_key_aqui";
            _resend = ResendClient.Create(apiKey);
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
                    HtmlBody = $@"
                        <html>
                            <body style='font-family: Arial, sans-serif;'>
                                <h2>Moneki</h2>
                                <p>{mensaje.Replace("\n", "<br/>")}</p>
                                <hr/>
                                <small>Mensaje automático, no responder.</small>
                            </body>
                        </html>",
                    TextBody = mensaje
                };

                var result = await _resend.EmailSendAsync(email);
                
                if (!result.IsSuccessful())
                {
                    Console.WriteLine($"❌ Error Resend: {result.Error?.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Email enviado a {correoDestino}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                throw;
            }
        }

        // Fire and forget - no bloquea la respuesta
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
