namespace PictureRenameApp.Tests
{
    public class AppSettingsTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "PictureRenameAppTests_" + Guid.NewGuid().ToString("N"));
        private string SettingsPath => Path.Combine(_dir, "sub", "settings.json");

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Fact]
        public void Load_MissingFile_ReturnsDefaults()
        {
            var s = AppSettings.Load(SettingsPath);
            Assert.True(s.BatchMoveToSubfolder);
            Assert.True(s.UseRecycleBin);
            Assert.True(s.ReopenLastFolder);
            Assert.Equal(2, s.DefaultZeroPad);
            Assert.Null(s.LastFolder);
        }

        [Fact]
        public void SaveThenLoad_RoundTrips()
        {
            var s = new AppSettings
            {
                BatchMoveToSubfolder = false,
                UseRecycleBin = false,
                ReopenLastFolder = false,
                DefaultZeroPad = 4,
                LastFolder = @"C:\Pictures",
            };
            s.Save(SettingsPath);

            var loaded = AppSettings.Load(SettingsPath);
            Assert.False(loaded.BatchMoveToSubfolder);
            Assert.False(loaded.UseRecycleBin);
            Assert.False(loaded.ReopenLastFolder);
            Assert.Equal(4, loaded.DefaultZeroPad);
            Assert.Equal(@"C:\Pictures", loaded.LastFolder);
        }

        [Fact]
        public void Load_CorruptFile_ReturnsDefaults()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, "{ not json");
            Assert.True(AppSettings.Load(SettingsPath).UseRecycleBin);
        }

        [Fact]
        public void Load_OutOfRangePad_IsClamped()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, "{ \"DefaultZeroPad\": 99 }");
            Assert.Equal(AppSettings.MaxZeroPad, AppSettings.Load(SettingsPath).DefaultZeroPad);
        }
    }
}
