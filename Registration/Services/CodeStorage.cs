using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Registration.Services
{
    /// <summary>
    /// —татический класс дл€ временного хранени€ кодов подтверждени€ (например, дл€ 2FA).
    /// ќбеспечивает потокобезопасность и автоматическую проверку срока действи€ кодов.
    /// </summary>
    public static class CodeStorage
    {
        /// <summary>
        /// ѕотокобезопасный словарь дл€ хранени€ кодов. 
        ///  люч Ч идентификатор (например, email), значение Ч кортеж из кода и времени истечени€.
        /// </summary>
        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> _codes =
            new ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)>();

        /// <summary>
        /// —охран€ет или обновл€ет код в хранилище с указанным временем жизни.
        /// </summary>
        /// <param name="key">”никальный идентификатор (логин или email).</param>
        /// <param name="code">√енерируемый код подтверждени€.</param>
        /// <param name="expiration">»нтервал времени, в течение которого код действителен.</param>
        public static void StoreCode(string key, string code, TimeSpan expiration)
        {
            // ¬ычисл€ем абсолютное врем€ истечени€ кода
            var expiresAt = DateTime.UtcNow.Add(expiration);

            // ћетод AddOrUpdate гарантирует корректную работу при одновременных запросах
            _codes.AddOrUpdate(key,
                addValueFactory: (k) => (code, expiresAt),
                updateValueFactory: (k, oldValue) => (code, expiresAt));
        }

        /// <summary>
        /// ѕытаетс€ извлечь код из хранилища. ≈сли срок действи€ истек, код удал€етс€.
        /// </summary>
        /// <param name="key"> люч дл€ поиска кода.</param>
        /// <param name="code">¬ыходной параметр, содержащий найденный код (или null).</param>
        /// <returns>True, если действительный код найден; иначе false.</returns>
        public static bool TryGetCode(string key, out string code)
        {
            if (_codes.TryGetValue(key, out var entry))
            {
                // ѕроверка: не истекло ли врем€ жизни кода
                if (DateTime.UtcNow > entry.ExpiresAt)
                {
                    // ”даление просроченного кода из пам€ти
                    _codes.TryRemove(key, out _);
                    code = null;
                    return false;
                }
                code = entry.Code;
                return true;
            }

            code = null;
            return false;
        }

        /// <summary>
        /// ѕровер€ет соответствие введенного пользователем кода сохраненному.
        /// ¬ случае успеха код удал€етс€ из хранилища (одноразовое использование).
        /// </summary>
        /// <param name="key"> люч пользовател€.</param>
        /// <param name="inputCode"> од, введенный пользователем.</param>
        /// <returns>True, если коды совпали и срок действи€ не истек; иначе false.</returns>
        public static bool ValidateCode(string key, string inputCode)
        {
            if (TryGetCode(key, out string storedCode))
            {
                // —равнение кодов
                if (storedCode == inputCode)
                {
                    // ”даление кода после успешной проверки дл€ предотвращени€ повторного использовани€
                    _codes.TryRemove(key, out _);
                    return true;
                }
            }
            return false;
        }
    }
}