using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ToolBarApp.Helpers
{
    public class ResizeModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ResizeMode mode)
            {
                return mode == ResizeMode.NoResize ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
