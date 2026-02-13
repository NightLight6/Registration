using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Registration.Services
{
    /// <summary>
    /// Сервис для отправки email через SMTP Yandex.
    /// </summary>
    public class EmailService
    {
        private readonly string _smtpHost = "smtp.yandex.ru";
        private readonly int _smtpPort = 587;
        private readonly string _fromEmail;
        private readonly string _password;

        /// <summary>
        /// Конструктор сервиса отправки email через Яндекс SMTP
        /// </summary>
        /// <param name="fromEmail">Email отправителя (должен быть @yandex.ru или @ya.ru)</param>
        /// <param name="password">Пароль приложения Яндекс (не основной пароль!)</param>
        public EmailService(string fromEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new ArgumentException("Email отправителя не может быть пустым", nameof(fromEmail));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не может быть пустым", nameof(password));

            _fromEmail = fromEmail;
            _password = password;
        }

        /// <summary>
        /// Асинхронно отправляет код подтверждения на email.
        /// </summary>
        /// <param name="toEmail">Email получателя</param>
        /// <param name="subject">Тема письма</param>
        /// <param name="code">Код подтверждения или текст сообщения</param>
        /// <returns>True если письмо отправлено успешно</returns>
        public async Task<bool> SendCodeAsync(string toEmail, string subject, string code)
        {
            try
            {
                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_fromEmail, _password);
                    client.EnableSsl = true;
                    client.Timeout = 30000; // 30 секунд таймаут

                    var message = new MailMessage
                    {
                        From = new MailAddress(_fromEmail),
                        Subject = subject,
                        Body = $"<h2>Код подтверждения</h2>" +
                               $"<p>Ваш код: <strong>{code}</strong></p>" +
                               $"<p>Код действителен 5 минут.</p>",
                        IsBodyHtml = true
                    };
                    message.To.Add(toEmail);

                    await client.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки письма: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправляет письмо для восстановления пароля с кодом подтверждения
        /// </summary>
        /// <param name="toEmail">Email получателя</param>
        /// <param name="code">Код восстановления пароля</param>
        /// <returns>True если письмо отправлено успешно</returns>
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string code)
        {
            try
            {
                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_fromEmail, _password);
                    client.EnableSsl = true;
                    client.Timeout = 30000;

                    var message = new MailMessage
                    {
                        From = new MailAddress(_fromEmail),
                        Subject = "Восстановление пароля",
                        Body = $"<h2>Восстановление пароля</h2>" +
                               $"<p>Вы запросили восстановление пароля для вашего аккаунта.</p>" +
                               $"<p>Ваш код восстановления: <strong style='font-size: 20px; color: #2196F3;'>{code}</strong></p>" +
                               $"<p>Введите этот код в форме восстановления пароля.</p>" +
                               $"<p><small>Код действителен 5 минут. Если вы не запрашивали восстановление пароля, проигнорируйте это письмо.</small></p>",
                        IsBodyHtml = true
                    };
                    message.To.Add(toEmail);

                    await client.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки письма восстановления пароля: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправляет письмо для двухфакторной аутентификации
        /// </summary>
        /// <param name="toEmail">Email получателя</param>
        /// <param name="code">Код двухфакторной аутентификации</param>
        /// <returns>True если письмо отправлено успешно</returns>
        public async Task<bool> SendTwoFactorCodeAsync(string toEmail, string code)
        {
            try
            {
                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_fromEmail, _password);
                    client.EnableSsl = true;
                    client.Timeout = 30000;

                    var message = new MailMessage
                    {
                        From = new MailAddress(_fromEmail),
                        Subject = "Код двухфакторной аутентификации",
                        Body = $"<h2>Двухфакторная аутентификация</h2>" +
                               $"<p>Для завершения входа в систему введите код подтверждения:</p>" +
                               $"<p style='font-size: 24px; font-weight: bold; color: #4CAF50; text-align: center;'>{code}</p>" +
                               $"<p>Код действителен 5 минут.</p>" +
                               $"<p><small>Если вы не пытались войти в систему, немедленно смените пароль.</small></p>",
                        IsBodyHtml = true
                    };
                    message.To.Add(toEmail);

                    await client.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки кода 2FA: {ex.Message}");
                return false;
            }
        }
    }
}