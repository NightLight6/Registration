using System;
using System.Globalization;
using System.Windows.Data;

namespace Registration.Helpers
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? (b ? "Вкл" : "Выкл") : "Н/Д";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}