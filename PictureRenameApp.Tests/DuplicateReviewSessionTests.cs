namespace PictureRenameApp.Tests
{
    public class DuplicateReviewSessionTests
    {
        private static DuplicateCandidate C(string path, long length = 100) =>
            new(path, length, DateTime.MinValue, "H", null, 10, 10);

        private static DuplicateReviewSession Session() => new(new[]
        {
            new DuplicateGroup(new[] { C("a1", 100), C("a2", 100), C("a3", 100) }, DuplicateKind.Exact),
            new DuplicateGroup(new[] { C("b1", 50), C("b2", 40) }, DuplicateKind.LookAlike),
        });

        [Fact]
        public void Keep_RecordsDecisionAndAdvances()
        {
            var s = Session();
            s.Keep("a2");
            Assert.Equal(1, s.Current);
            var d = Assert.Single(s.Decisions);
            Assert.Equal("a2", d.Keep.Path);
            Assert.Equal(new[] { "a1", "a3" }, d.Remove.Select(f => f.Path));
            Assert.Equal(2, s.FilesToDelete);
            Assert.Equal(200, s.BytesToDelete);
        }

        [Fact]
        public void Skip_KeepsEverything()
        {
            var s = Session();
            s.Skip();
            s.Keep("b1");
            Assert.True(s.IsFinished);
            Assert.Equal("b1", Assert.Single(s.Decisions).Keep.Path);
            Assert.Equal(1, s.GroupsLeftAlone);
        }

        [Fact]
        public void Back_AllowsChangingAnEarlierChoice()
        {
            var s = Session();
            s.Keep("a1");
            s.Back();
            Assert.Equal(0, s.Current);
            Assert.Equal("a1", s.ChoiceFor(0));
            s.Skip();
            Assert.Empty(s.Decisions);
        }

        [Fact]
        public void Back_AtStartDoesNothing()
        {
            var s = Session();
            s.Back();
            Assert.Equal(0, s.Current);
            Assert.False(s.CanGoBack);
        }

        [Fact]
        public void Keep_FileOutsideGroupThrows()
        {
            Assert.Throws<ArgumentException>(() => Session().Keep("b1"));
        }

        [Fact]
        public void Keep_AfterFinishThrows()
        {
            var s = Session();
            s.Skip(); s.Skip();
            Assert.Throws<InvalidOperationException>(() => s.Keep("a1"));
        }
    }
}
