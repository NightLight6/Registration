using System;
using System.Security.Cryptography;
using System.Text;

namespace Registration.Services
{
    /// <summary>
    /// Сервис для хэширования паролей с использованием SHA256
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Вычисляет SHA256 хэш для строки
        /// </summary>
        /// <param name="rawData">Исходная строка (пароль)</param>
        /// <returns>Хэш в hex-формате</returns>
        public static string ComputeSha256Hash(string rawData)
        {
            if (string.IsNullOrEmpty(rawData))
                throw new ArgumentException("Пароль не может быть пустым", nameof(rawData));

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Вычисляем хэш
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Конвертируем байты в hex-строку
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Проверяет соответствие пароля и хэша
        /// </summary>
        /// <param name="password">Пароль для проверки</param>
        /// <param name="hash">Ожидаемый хэш</param>
        /// <returns>True если пароль верен</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            string computedHash = ComputeSha256Hash(password);
            return computedHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}