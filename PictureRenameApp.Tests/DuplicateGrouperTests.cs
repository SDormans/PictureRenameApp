namespace PictureRenameApp.Tests
{
    public class DuplicateGrouperTests
    {
        private static readonly DateTime T0 = new(2024, 1, 1);

        private static DuplicateCandidate C(string name, string? content = null, ulong? visual = null,
            int w = 100, int h = 100, long length = 1000, int ageDays = 0) =>
            new(Path.Combine("dir", name), length, T0.AddDays(ageDays), content, visual, w, h);

        [Fact]
        public void Group_SameContentIsExactGroup()
        {
            var groups = DuplicateGrouper.Group(new[] { C("a.jpg", "H1"), C("b.jpg", "H1"), C("c.jpg", "H2") });
            var g = Assert.Single(groups);
            Assert.Equal(DuplicateKind.Exact, g.Kind);
            Assert.Equal(2, g.Files.Count);
        }

        [Fact]
        public void Group_CloseVisualHashesAreLookAlikes()
        {
            var groups = DuplicateGrouper.Group(new[] { C("a.jpg", visual: 0b0000), C("b.jpg", visual: 0b0111), C("c.jpg", visual: ulong.MaxValue) }, maxDistance: 3);
            var g = Assert.Single(groups);
            Assert.Equal(DuplicateKind.LookAlike, g.Kind);
            Assert.Equal(new[] { "a.jpg", "b.jpg" }, g.Files.Select(f => Path.GetFileName(f.Path)).OrderBy(n => n));
        }

        [Fact]
        public void Group_DistanceAboveThresholdIsNotGrouped()
        {
            Assert.Empty(DuplicateGrouper.Group(new[] { C("a.jpg", visual: 0b0000), C("b.jpg", visual: 0b1111) }, maxDistance: 3));
        }

        [Fact]
        public void Group_MatchesAreTransitive()
        {
            var groups = DuplicateGrouper.Group(new[] { C("a.jpg", visual: 0b000000), C("b.jpg", visual: 0b000111), C("c.jpg", visual: 0b111111) }, maxDistance: 3);
            Assert.Equal(3, Assert.Single(groups).Files.Count);
        }

        [Fact]
        public void Group_ExactCopyAndResizedVersionFormOneLookAlikeGroup()
        {
            var groups = DuplicateGrouper.Group(new[]
            {
                C("a.jpg", "H1", 0b0), C("a copy.jpg", "H1", 0b0), C("a small.jpg", "H2", 0b1, w: 50, h: 50),
            });
            var g = Assert.Single(groups);
            Assert.Equal(DuplicateKind.LookAlike, g.Kind);
            Assert.Equal(3, g.Files.Count);
        }

        [Fact]
        public void Group_FilesWithoutHashesAreIgnored()
        {
            Assert.Empty(DuplicateGrouper.Group(new[] { C("a.txt"), C("b.txt") }));
        }

        [Fact]
        public void Group_ExactGroupsComeFirst()
        {
            var groups = DuplicateGrouper.Group(new[]
            {
                C("a.jpg", visual: 0b0), C("b.jpg", visual: 0b1),
                C("x.jpg", "H1"), C("y.jpg", "H1"),
            });
            Assert.Equal(new[] { DuplicateKind.Exact, DuplicateKind.LookAlike }, groups.Select(g => g.Kind));
        }

        [Fact]
        public void Best_PrefersResolutionThenSizeThenShortNameThenOldest()
        {
            Assert.Equal("big.jpg", Path.GetFileName(DuplicateGrouper.OrderByPreference(new[] { C("small.jpg", w: 10), C("big.jpg", w: 200) }).First().Path));
            Assert.Equal("heavy.jpg", Path.GetFileName(DuplicateGrouper.OrderByPreference(new[] { C("light.jpg", length: 10), C("heavy.jpg", length: 20) }).First().Path));
            Assert.Equal("a.jpg", Path.GetFileName(DuplicateGrouper.OrderByPreference(new[] { C("a (2).jpg"), C("a.jpg") }).First().Path));
            Assert.Equal("b.jpg", Path.GetFileName(DuplicateGrouper.OrderByPreference(new[] { C("a.jpg", ageDays: 5), C("b.jpg", ageDays: 1) }).First().Path));
        }
    }
}
