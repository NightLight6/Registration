using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registration.Services
{
    public static class TimeHelper
    {
        /// <summary>
        /// Возвращает приветствие по времени суток
        /// </summary>
        public static string GetGreeting()
        {
            int hour = DateTime.Now.Hour;

            if (hour >= 5 && hour < 12)
                return "Доброе утро";
            else if (hour >= 12 && hour < 17)
                return "Добрый день";
            else if (hour >= 17 && hour < 20)
                return "Добрый вечер";
            else
                return "Доброй ночи";
        }

        /// <summary>
        /// Проверяет, находится ли текущее время в рабочих часах (10:00–19:00)
        /// </summary>
        public static bool IsWorkTime()
        {
            var now = DateTime.Now;
            var start = new TimeSpan(10, 0, 0); // 10:00
            var end = new TimeSpan(13, 0, 0);   // 19:00

            var currentTime = now.TimeOfDay;

            return currentTime >= start && currentTime < end;
        }
    }
}
