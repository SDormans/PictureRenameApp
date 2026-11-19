namespace PictureRenameApp.Tests
{
    public class RenameOperationTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "PictureRenameAppTests_" + Guid.NewGuid().ToString("N"));

        public RenameOperationTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        private string Touch(string name)
        {
            var p = Path.Combine(_dir, name);
            File.WriteAllText(p, name);
            return p;
        }

        [Fact]
        public void Undo_RestoresMovesAndRemovesCreatedDirectory()
        {
            var a = Touch("a.jpg");
            var b = Touch("b.jpg");
            var sub = Path.Combine(_dir, "Holiday");
            var op = new RenameOperation();
            op.EnsureDirectory(sub);
            var a2 = Path.Combine(sub, "Holiday 01.jpg");
            var b2 = Path.Combine(sub, "Holiday 02.jpg");
            File.Move(a, a2); op.AddMove(a, a2);
            File.Move(b, b2); op.AddMove(b, b2);

            var result = op.Undo();

            Assert.Equal(2, result.Restored.Count);
            Assert.Equal(0, result.Failed);
            Assert.True(File.Exists(a));
            Assert.True(File.Exists(b));
            Assert.False(Directory.Exists(sub));
        }

        [Fact]
        public void Undo_SkipsWhenOriginalNameIsTaken()
        {
            var a = Touch("a.jpg");
            var a2 = Path.Combine(_dir, "renamed.jpg");
            var op = new RenameOperation();
            File.Move(a, a2); op.AddMove(a, a2);
            Touch("a.jpg");

            var result = op.Undo();

            Assert.Empty(result.Restored);
            Assert.Equal(1, result.Failed);
            Assert.True(File.Exists(a2));
        }

        [Fact]
        public void Undo_KeepsCreatedDirectoryThatIsNotEmpty()
        {
            var sub = Path.Combine(_dir, "Keep");
            var op = new RenameOperation();
            op.EnsureDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "other.txt"), "x");

            op.Undo();

            Assert.True(Directory.Exists(sub));
        }

        [Fact]
        public void EnsureDirectory_ExistingDirectoryIsNotRemovedOnUndo()
        {
            var op = new RenameOperation();
            op.EnsureDirectory(_dir);
            op.Undo();
            Assert.True(Directory.Exists(_dir));
        }
    }
}
