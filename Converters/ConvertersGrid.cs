using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace OsEngine.Converters
{
    public class ConverterColorToSide : IValueConverter
    {
        /// <summary>
        /// Возвращает "цвет" взависимости от параметра value (buy/sell)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush color = Brushes.White;

            if (value is Side )
            {
                if ( (Side)value == Side.Buy)
                {
                    color = Brushes.DarkGreen;
                }
                else
                {
                    color = Brushes.OrangeRed;
                }
            }

            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConverterIsRunToBool : IValueConverter
    {
        /// <summary>
        /// Возвращает "START/STOP" взависимости от параметра value (true/false)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = "START";

            if (value is bool)
            {
                if ((bool)value == true)
                {
                    str = "STOP";
                }
            }

            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
