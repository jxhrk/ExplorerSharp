using Avalonia.Data.Converters;
using Avalonia.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplorerSharp.Converters
{
    internal class FileSizeConverter : IValueConverter
    {
        public static readonly FileSizeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter,
                                                                CultureInfo culture)
        {
            if (value is long size
                && targetType.IsAssignableTo(typeof(string)))
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = size;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                return String.Format("{0:0.##} {1}", len, sizes[order]);
            }
            // converter used for the wrong type
            return new BindingNotification(new InvalidCastException(),
                                                    BindingErrorType.Error);
        }

        public object ConvertBack(object? value, Type targetType,
                                    object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

