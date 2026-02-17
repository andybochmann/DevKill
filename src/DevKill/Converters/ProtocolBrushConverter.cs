using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DevKill.Converters;

public class ProtocolBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush TcpBrush = new(Color.FromRgb(96, 165, 250));   // Blue-400
    private static readonly SolidColorBrush UdpBrush = new(Color.FromRgb(251, 191, 36));   // Amber-400

    static ProtocolBrushConverter()
    {
        TcpBrush.Freeze();
        UdpBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string protocol && protocol == "UDP" ? UdpBrush : TcpBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
