using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Proyecto_servicio.Helpers
{
    public class EmailService
    {
        private readonly string _apiKey;
        private readonly string _senderEmail = "ipncecyt13informatica.pa@gmail.com";
        private readonly string _senderName = "Moneki";
        private readonly HttpClient _httpClient;

        public EmailService()
        {
            _apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
            _httpClient = new HttpClient();
        }

        // Usamos Task directamente sin ambigüedad
        public async Task<bool> EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    Console.WriteLine("⚠️ BREVO_API_KEY no configurada");
                    return false;
                }

                var requestBody = new
                {
                    sender = new { name = _senderName, email = _senderEmail },
                    to = new[] { new { email = correoDestino } },
                    subject = asunto,
                    htmlContent = $@"
                        <!DOCTYPE html>
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2 style='color: #2563eb;'>Moneki</h2>
                            <p>{mensaje.Replace("\n", "<br/>")}</p>
                            <hr/>
                            <small style='color: #666;'>Este es un mensaje automático, por favor no responder.</small>
                        </body>
                        </html>"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("accept", "application/json");

                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Email enviado a {correoDestino}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Error Brevo: {response.StatusCode} - {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error enviando email: {ex.Message}");
                return false;
            }
        }

        // Fire-and-forget
        public void EnviarCorreoEnBackground(string correoDestino, string asunto, string mensaje)
        {
            _ = Task.Run(async () =>
            {
                await EnviarCorreoAsync(correoDestino, asunto, mensaje);
            });
        }
    }
}
