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
        public void SubfolderName_RemovesInvalidFileNameChars()
        {
            var name = RenamePattern.SubfolderName("a/b {n}");
            Assert.DoesNotContain('/', name);
        }
    }
}
