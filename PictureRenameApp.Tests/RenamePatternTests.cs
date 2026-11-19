namespace PictureRenameApp.Tests
{
    public class RenamePatternTests
    {
        [Theory]
        [InlineData("file {n}", 1, 2, ".jpg", "file 01.jpg")]
        [InlineData("Holiday-{n}", 7, 3, ".png", "Holiday-007.png")]
        [InlineData("img", 12, 2, ".jpg", "img12.jpg")]
        [InlineData("{n} - trip", 3, 1, ".gif", "3 - trip.gif")]
        public void Format_ReplacesNumberToken(string pattern, int number, int pad, string ext, string expected)
        {
            Assert.Equal(expected, RenamePattern.Format(pattern, number, pad, ext));
        }

        [Theory]
        [InlineData("Holiday {n}", "Holiday")]
        [InlineData("Holiday ({n})", "Holiday")]
        [InlineData("trip_{n}_2024", "trip__2024")]
        [InlineData("{n}", RenamePattern.DefaultSubfolder)]
        [InlineData("  {n} - ", RenamePattern.DefaultSubfolder)]
        public void SubfolderName_StripsNumberTokenAndSeparators(string pattern, string expected)
        {
            Assert.Equal(expected, RenamePattern.SubfolderName(pattern));
        }

        [Fact]
        public void Format_ExpandsNameAndDateTokens()
        {
            var item = new RenameItem("IMG_1234", ".jpg", new DateTime(2024, 5, 1, 13, 45, 0));
            Assert.Equal("2024-05-01 IMG_1234 01.jpg", RenamePattern.Format("{date} {name} {n}", 1, 2, item));
            Assert.Equal("20240501_3.jpg", RenamePattern.Format("{date:yyyyMMdd}_{n}", 3, 1, item));
        }

        [Fact]
        public void Format_DateTokenWithInvalidCharsIsSanitized()
        {
            var item = new RenameItem("x", ".jpg", new DateTime(2024, 5, 1, 13, 45, 0));
            Assert.Equal("2024-05-01 13-45 1.jpg", RenamePattern.Format("{date:yyyy/MM/dd HH:mm} {n}", 1, 1, item));
        }

        [Fact]
        public void SubfolderName_StripsAllTokens()
        {
            Assert.Equal("Holiday", RenamePattern.SubfolderName("Holiday {date} {name} {n}"));
        }

        [Fact]
        public void SubfolderName_RemovesInvalidFileNameChars()
        {
            var name = RenamePattern.SubfolderName("a/b {n}");
            Assert.DoesNotContain('/', name);
        }
    }
}
