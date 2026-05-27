using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace Proyecto_servicio.Helpers
{
    public class EmailService
    {
        private readonly TransactionalEmailsApi _api;
        private readonly string _senderEmail = "ipncecyt13informatica.pa@gmail.com";
        private readonly string _senderName = "Moneki";

        public EmailService()
        {
            var apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("BREVO_API_KEY no configurada en variables de entorno");
            }
            
            // Configurar cliente de Brevo
            Configuration.Default.ApiKey.Add("api-key", apiKey);
            _api = new TransactionalEmailsApi();
        }

        public async Task<bool> EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
        {
            try
            {
                // Crear el mensaje
                var sendSmtpEmail = new SendSmtpEmail();
                sendSmtpEmail.Subject = asunto;
                sendSmtpEmail.HtmlContent = $@"
                    <!DOCTYPE html>
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2 style='color: #2563eb;'>Moneki</h2>
                        <p>{mensaje.Replace("\n", "<br/>")}</p>
                        <hr/>
                        <small style='color: #666;'>Este es un mensaje automático, por favor no responder.</small>
                    </body>
                    </html>";
                
                sendSmtpEmail.Sender = new SendSmtpEmailSender(_senderName, _senderEmail);
                sendSmtpEmail.To = new List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(correoDestino)
                };

                // Enviar mediante API de Brevo (Puerto 443 - No bloqueado por Render)
                var result = await _api.SendTransacEmailAsync(sendSmtpEmail);
                
                Console.WriteLine($"✅ Email enviado a {correoDestino}, Mensaje ID: {result.MessageId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error Brevo: {ex.Message}");
                return false;
            }
        }

        // Método fire-and-forget (no bloquea la respuesta de tu API)
        public void EnviarCorreoEnBackground(string correoDestino, string asunto, string mensaje)
        {
            _ = Task.Run(async () =>
            {
                await EnviarCorreoAsync(correoDestino, asunto, mensaje);
            });
        }
    }
}
