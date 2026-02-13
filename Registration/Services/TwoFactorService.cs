using Registration.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Services
{
    /// <summary>
    /// Сервис для работы с двухфакторной аутентификацией
    /// Позволяет легко включать/отключать 2FA для пользователей
    /// </summary>
    public class TwoFactorService
    {
        private readonly EmailService _emailService;

        /// <summary>
        /// Конструктор сервиса двухфакторной аутентификации
        /// </summary>
        /// <param name="emailService">Сервис отправки email</param>
        public TwoFactorService(EmailService emailService)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        /// <summary>
        /// Включает двухфакторную аутентификацию для пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>True если операция успешна</returns>
        public bool EnableTwoFactorAuth(int userId)
        {
            using (var context = new BeermageEntities1())
            {
                var user = context.Users.FirstOrDefault(u => u.UserID == userId);
                if (user != null)
                {
                    user.IsTwoFactorEnabled = true;
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Отключает двухфакторную аутентификацию для пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>True если операция успешна</returns>
        public bool DisableTwoFactorAuth(int userId)
        {
            using (var context = new BeermageEntities1())
            {
                var user = context.Users.FirstOrDefault(u => u.UserID == userId);
                if (user != null)
                {
                    user.IsTwoFactorEnabled = false;
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Проверяет статус двухфакторной аутентификации
        /// </summary>
        public bool IsTwoFactorEnabled(int userId)
        {
            using (var context = new BeermageEntities1())
            {
                var user = context.Users.FirstOrDefault(u => u.UserID == userId);
                return user?.IsTwoFactorEnabled ?? false;
            }
        }

        /// <summary>
        /// Отправляет код двухфакторной аутентификации
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Сгенерированный код</returns>
        public async Task<string> SendTwoFactorCodeAsync(int userId)
        {
            using (var context = new BeermageEntities1())
            {
                var user = context.Users.FirstOrDefault(u => u.UserID == userId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    throw new InvalidOperationException("Пользователь не найден или email не указан");

                string code = GenerateRandomCode();
                CodeStorage.StoreCode(userId.ToString(), code, TimeSpan.FromMinutes(5));

                bool sent = await _emailService.SendTwoFactorCodeAsync(
                    user.Email,
                    code
                );

                if (!sent)
                {
                    throw new InvalidOperationException("Не удалось отправить код 2FA");
                }

                return code;
            }
        }

        /// <summary>
        /// Проверяет код двухфакторной аутентификации
        /// </summary>
        public bool VerifyTwoFactorCode(int userId, string inputCode)
        {
            return CodeStorage.ValidateCode(userId.ToString(), inputCode);
        }

        /// <summary>
        /// Генерирует случайный 4-значный код
        /// </summary>
        private string GenerateRandomCode()
        {
            Random random = new Random();
            return random.Next(1000, 9999).ToString();
        }
    }
}