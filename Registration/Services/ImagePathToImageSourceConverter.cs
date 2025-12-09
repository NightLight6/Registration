using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Registration.Services
{
    public class ImagePathToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    if (Path.IsPathRooted(imagePath))
                    {
                        return new BitmapImage(new Uri(imagePath));
                    }
                    else
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                        if (File.Exists(fullPath))
                        {
                            return new BitmapImage(new Uri(fullPath));
                        }
                    }
                }
                catch
                {
                }
            }
            return new BitmapImage(new Uri("pack://application:,,,/Images/no_product.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}