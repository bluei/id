using System;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

using System.Windows.Media;                             // solid color brushes

//using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;


namespace LightSwitchApplication
{
      
    public static class POStatusInfo
    {
        // this is populated via the calling screen.  
        public static IEnumerable<PurchaseOrderStatus> POStatuses;
    }
    
    #region ##### CONVERTERS #####
    public class FontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // set font size to return here
            //return 8;
            int i = System.Convert.ToInt32(value);
            if (i > 77)
            {
                return 18;
            }
            else
            {
                return 8;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ChangeFontColorStatus : IValueConverter
    {
        //  class to change colors
        //  --------------------------------------------------------------------------------------------------------

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                string color = value.ToString();

                switch (color)
                {
                    case "Blue":
                        return new SolidColorBrush(Colors.Blue);
                    case "Cyan":
                        return new SolidColorBrush(Colors.Cyan);
                    case "DarkGray":
                        return new SolidColorBrush(Colors.DarkGray);
                    case "Gray":
                        return new SolidColorBrush(Colors.Gray);
                    case "Green":
                        return new SolidColorBrush(Colors.Green);
                    case "LightGray":
                        return new SolidColorBrush(Colors.LightGray);
                    case "Orange":
                        return new SolidColorBrush(Colors.Orange);
                    case "Purple":
                        return new SolidColorBrush(Colors.Purple);
                    case "Red":
                        return new SolidColorBrush(Colors.Red);
                    case "Yellow":
                        return new SolidColorBrush(Colors.Yellow);
                    default:
                        return new SolidColorBrush(Colors.Black);
                }
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class BgStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            Color color = Colors.Transparent;

            if (value != null && value.GetType() == typeof(string))
            {
                string name = value.ToString();
                switch (name.ToLower())
                {
                    case "red": color = Colors.Red; break;
                    case "green": color = Colors.Green; break;
                    case "blue": color = Colors.Blue; break;
                    case "yellow": color = Colors.Yellow; break;
                    case "cyan": color = Colors.Cyan; break;
                    case "brown": color = Colors.Brown; break;
                    case "orange": color = Colors.Orange; break;
                    case "gray": color = Colors.Gray; break;
                    case "darkgray": color = Colors.DarkGray; break;
                    case "lightgray": color = Colors.LightGray; break;
                    case "magenta": color = Colors.Magenta; break;
                    case "purple": color = Colors.Purple; break;
                    //case "white":
                    default: color = Colors.Transparent; break;
                };
                if (param != null && param.GetType() == typeof(byte))
                {
                    color.A = (byte)param;
                }
                else
                    color.A = 64; // Set opacity to 25%
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object param,
            CultureInfo culture)
        {
            return null;
        }
    }

    public class LateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            Color color = Colors.Transparent;

            if (value != null && value.GetType() == typeof(int))
            {
                if ((int)value < 0) color = Colors.Red;

                if (param != null && param.GetType() == typeof(byte))
                {
                    color.A = (byte)param;
                }
                else
                    color.A = 128; // Set opacity to 50%
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object param,
            CultureInfo culture)
        {
            return null;
        }
    }

    public class HighlightColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            Color color = Colors.Transparent;

            if (value != null && value.GetType() == typeof(bool) && (bool)value == true)
            {
                color = Colors.Yellow;
                color.A = 128; // Set opacity to 50%         
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object param,
            CultureInfo culture)
        {
            return null;
        }
    }

    #endregion
}