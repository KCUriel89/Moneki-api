using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System;
using System.Threading.Tasks;

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
                Console.WriteLine("⚠️ BREVO_API_KEY no configurada");
                return;
            }
            
            Configuration.Default.ApiKey.Add("api-key", apiKey);
            _api = new TransactionalEmailsApi();
        }

        // Usamos 'System.Threading.Tasks.Task' explícitamente para evitar conflicto
        public async System.Threading.Tasks.Task<bool> EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
        {
            try
            {
                if (_api == null)
                {
                    Console.WriteLine("❌ Brevo no configurado");
                    return false;
                }

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

                var result = await _api.SendTransacEmailAsync(sendSmtpEmail);
                
                Console.WriteLine($"✅ Email enviado a {correoDestino}, ID: {result.MessageId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error Brevo: {ex.Message}");
                return false;
            }
        }

        // Fire-and-forget usando la sintaxis correcta
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
