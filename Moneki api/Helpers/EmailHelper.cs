using MailKit.Security;
using Moneki_api.Services;
using MailKit.Net.Smtp;
using Npgsql;
using NpgsqlTypes;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_servicio.Helpers
{
   public class EmailService
{
    private readonly string correoOrigen = "ipncecyt13informatica.pa@gmail.com";
    private readonly string contraseñaApp = "frut jfbb nuys lcci";
    private readonly string servidor = "smtp.gmail.com";
    private readonly int puerto = 587;

    public EmailService()
    {
    }

    // Método que no espera a que termine (fire and forget)
    public void EnviarCorreoEnBackground(string correoDestino, string asunto, string mensaje)
    {
        Task.Run(async () => 
        {
            try
            {
                await EnviarCorreoAsync(correoDestino, asunto, mensaje);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando email a {correoDestino}: {ex.Message}");
            }
        });
    }

    public async Task EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(correoOrigen));
            email.To.Add(MailboxAddress.Parse(correoDestino));
            email.Subject = asunto;
            email.Body = new TextPart("plain") { Text = mensaje };

            using var smtp = new SmtpClient();
            
            // Configurar timeouts más largos para Render
            smtp.Timeout = 30000; // 30 segundos
            
            await smtp.ConnectAsync(servidor, puerto, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(correoOrigen, contraseñaApp);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
            
            Console.WriteLine($"✅ Email enviado a {correoDestino}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error SMTP: {ex.Message}");
            throw;
        }
    }
}
}
