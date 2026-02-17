using System.Globalization;
using System.Windows.Media;
using DevKill.Converters;
using Xunit;

namespace DevKill.Tests;

public class ProtocolBrushConverterTests
{
    private readonly ProtocolBrushConverter _converter = new();

    [Fact]
    public void Convert_TCP_ReturnsBlueBrush()
    {
        var result = _converter.Convert("TCP", typeof(Brush), null!, CultureInfo.InvariantCulture);

        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.FromRgb(96, 165, 250), brush.Color);
    }

    [Fact]
    public void Convert_UDP_ReturnsAmberBrush()
    {
        var result = _converter.Convert("UDP", typeof(Brush), null!, CultureInfo.InvariantCulture);

        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.FromRgb(251, 191, 36), brush.Color);
    }

    [Fact]
    public void Convert_Null_ReturnsTcpBrush()
    {
        var result = _converter.Convert(null!, typeof(Brush), null!, CultureInfo.InvariantCulture);

        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.FromRgb(96, 165, 250), brush.Color);
    }

    [Fact]
    public void Convert_UnknownString_ReturnsTcpBrush()
    {
        var result = _converter.Convert("SCTP", typeof(Brush), null!, CultureInfo.InvariantCulture);

        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.FromRgb(96, 165, 250), brush.Color);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(null!, typeof(string), null!, CultureInfo.InvariantCulture));
    }
}
