using System;
using Xunit;
using ComicReader.Converters;
using ComicReader.Services;

namespace ComicReader.Tests
{
    public class ConvertersTests
    {
        [Fact]
        public void BoolToVisibility_ConvertAndBack()
        {
            var conv = new BoolToVisibilityConverter();
            var v = conv.Convert(true, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture);
            // Compare string representation to avoid a compile-time dependency on WPF assemblies
            Assert.Equal("Visible", v.ToString());
            var back = conv.ConvertBack(v, typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(back is bool && (bool)back);
        }

        [Fact]
        public void InverseBoolToVisibility_ConvertAndBack()
        {
            var conv = new InverseBoolToVisibilityConverter();
            var v = conv.Convert(true, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal("Collapsed", v.ToString());
            var back = conv.ConvertBack(v, typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(back is bool && (bool)back);
        }

        [Fact]
        public void PercentageToWidth_ConvertAndBack()
        {
            var conv = new PercentageToWidthConverter();
            var width = conv.Convert(50.0, typeof(double), "200", System.Globalization.CultureInfo.InvariantCulture);
            Assert.IsType<double>(width);
            Assert.Equal(100.0, (double)width, 3);
            var back = conv.ConvertBack(100.0, typeof(double), "200", System.Globalization.CultureInfo.InvariantCulture);
            Assert.IsType<double>(back);
            Assert.Equal(50.0, (double)back, 3);
        }

        [Fact]
        public void ReadingDirectionToFlowDirection_ConvertAndBack()
        {
            var conv = new ComicReader.Converters.ReadingDirectionToFlowDirectionConverter();
            var fd = conv.Convert(ReadingDirection.RightToLeft, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal("RightToLeft", fd.ToString());
            var back = conv.ConvertBack(fd, typeof(ReadingDirection), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(ReadingDirection.RightToLeft, back);
        }
    }
}
