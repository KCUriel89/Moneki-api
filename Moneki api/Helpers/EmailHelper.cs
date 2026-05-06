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
        private readonly DatabaseService _db;

        private readonly string correoOrigen = "ipncecyt13informatica.pa@gmail.com";
        private readonly string contraseñaApp = "frut jfbb nuys lcci";
        private readonly string servidor = "smtp.gmail.com";
        private readonly int puerto = 587;

        public EmailService()
        {
            _db = new DatabaseService();
        }

        public async Task EnviarCorreoAsync(string correoDestino, string asunto, string mensaje)
        {
            // ✅ 1. Crear correo
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(correoOrigen));
            email.To.Add(MailboxAddress.Parse(correoDestino));
            email.Subject = asunto;

            email.Body = new TextPart("plain")
            {
                Text = mensaje
            };

            // ✅ 2. Enviar por SMTP
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(servidor, puerto, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(correoOrigen, contraseñaApp);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            // ✅ 3. Guardar registro en PostgreSQL
            string query = @"INSERT INTO CorreosEnviados (Destino, Asunto, Fecha)
                             VALUES (@d, @a, NOW())";

            await _db.ExecuteAsync(query,
                new NpgsqlParameter("@d", correoDestino),
                new NpgsqlParameter("@a", asunto)
            );
        }
    }
}
