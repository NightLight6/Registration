using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Registration.Services
{
    public static class CodeStorage
    {
        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> _codes = new ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)>();


        public static void StoreCode(string key, string code, TimeSpan expiration)
        {
            var expiresAt = DateTime.UtcNow.Add(expiration);
            _codes.AddOrUpdate(key,
                addValueFactory: (k) => (code, expiresAt),
                updateValueFactory: (k, oldValue) => (code, expiresAt));
        }


        public static bool TryGetCode(string key, out string code)
        {
            if (_codes.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow > entry.ExpiresAt)
                {
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

        public static bool ValidateCode(string key, string inputCode)
        {
            if (TryGetCode(key, out string storedCode))
            {
                if (storedCode == inputCode)
                {
                    _codes.TryRemove(key, out _);
                    return true;
                }
            }
            return false;
        }
    }
}