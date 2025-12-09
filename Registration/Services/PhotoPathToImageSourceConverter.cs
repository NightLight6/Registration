using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Registration.Services
{
    public class PhotoPathToImageSourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string photoPath && !string.IsNullOrEmpty(photoPath))
            {
                try
                {
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photoPath);
                    return new BitmapImage(new Uri(fullPath));
                }
                catch
                {
                    return GetDefaultImage();
                }
            }
            else
            { 
                return GetDefaultImage();
            }
        }

        private static BitmapImage GetDefaultImage()
        {
            return new BitmapImage(new Uri("/Images/default.jpg", UriKind.RelativeOrAbsolute));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
