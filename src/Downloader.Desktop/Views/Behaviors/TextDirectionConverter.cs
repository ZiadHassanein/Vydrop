using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace YouTubeDownloader;

public sealed class TextDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (char character in value as string ?? "")
            if (char.IsLetter(character))
                return character is >= '\u0590' and <= '\u08ff' or >= '\ufb1d' and <= '\ufdff' or >= '\ufe70' and <= '\ufeff'
                    ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        return FlowDirection.LeftToRight;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
