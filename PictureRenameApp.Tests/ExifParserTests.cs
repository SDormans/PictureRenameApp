using System.Text;

namespace PictureRenameApp.Tests
{
    public class ExifParserTests
    {
        private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s + "\0");

        [Fact]
        public void ParseDate_ParsesExifFormat()
        {
            Assert.Equal(new DateTime(2023, 7, 14, 9, 30, 5), ExifParser.ParseDate(Ascii("2023:07:14 09:30:05")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("0000:00:00 00:00:00")]
        [InlineData("not a date")]
        public void ParseDate_InvalidReturnsNull(string value)
        {
            Assert.Null(ExifParser.ParseDate(Ascii(value)));
        }

        [Fact]
        public void ParseAscii_TrimsNullsAndWhitespace()
        {
            Assert.Equal("Canon", ExifParser.ParseAscii(Ascii("Canon  ")));
            Assert.Null(ExifParser.ParseAscii(null));
        }

        [Theory]
        [InlineData("Canon", "Canon EOS R6", "Canon EOS R6")]
        [InlineData("SONY", "ILCE-7M3", "SONY ILCE-7M3")]
        [InlineData(null, "Pixel 8", "Pixel 8")]
        [InlineData("Apple", null, "Apple")]
        [InlineData(null, null, null)]
        public void CameraName_CombinesMakeAndModel(string? make, string? model, string? expected)
        {
            Assert.Equal(expected, ExifParser.CameraName(make, model));
        }
    }
}
