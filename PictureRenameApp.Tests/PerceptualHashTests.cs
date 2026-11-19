namespace PictureRenameApp.Tests
{
    public class PerceptualHashTests
    {
        private static byte[] Image(Func<int, int, int> f)
        {
            var lum = new byte[PerceptualHash.Width * PerceptualHash.Height];
            for (int y = 0; y < PerceptualHash.Height; y++)
                for (int x = 0; x < PerceptualHash.Width; x++)
                    lum[y * PerceptualHash.Width + x] = (byte)Math.Clamp(f(x, y), 0, 255);
            return lum;
        }

        [Fact]
        public void FromLuminance_DarkeningToTheRightSetsAllBits()
        {
            Assert.Equal(ulong.MaxValue, PerceptualHash.FromLuminance(Image((x, _) => 200 - x * 20)));
        }

        [Fact]
        public void FromLuminance_BrighteningToTheRightSetsNoBits()
        {
            Assert.Equal(0UL, PerceptualHash.FromLuminance(Image((x, _) => 20 + x * 20)));
        }

        [Fact]
        public void FromLuminance_FlatImageReturnsNull()
        {
            Assert.Null(PerceptualHash.FromLuminance(Image((x, y) => 100 + (x + y) % 3)));
        }

        [Fact]
        public void FromLuminance_SmallBrightnessShiftKeepsHash()
        {
            var a = PerceptualHash.FromLuminance(Image((x, y) => (x * 37 + y * 91) % 200));
            var b = PerceptualHash.FromLuminance(Image((x, y) => (x * 37 + y * 91) % 200 + 30));
            Assert.Equal(a, b);
        }

        [Fact]
        public void FromLuminance_WrongSizeThrows()
        {
            Assert.Throws<ArgumentException>(() => PerceptualHash.FromLuminance(new byte[10]));
        }

        [Theory]
        [InlineData(0UL, 0UL, 0)]
        [InlineData(0UL, 0b1011UL, 3)]
        [InlineData(ulong.MaxValue, 0UL, 64)]
        public void Distance_CountsDifferentBits(ulong a, ulong b, int expected)
        {
            Assert.Equal(expected, PerceptualHash.Distance(a, b));
        }
    }
}
