using BiomedicalSignalPlotter.Arduino;

namespace BiomedicalSignalPlotter.Tests;

public class FirmwareInfoServiceTests
{
    [Fact]
    public void ResolveDefaultSketchFolderPath_ReturnsExistingFirmwareFolder()
    {
        string folderPath = FirmwareInfoService.ResolveDefaultSketchFolderPath();

        Assert.True(Directory.Exists(folderPath));
        Assert.EndsWith(
            Path.Combine("firmware", "arduino", "TwoChannelCsvStreamer"),
            folderPath,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadSketchAsync_ReturnsFirmwareSource()
    {
        using TemporaryFirmwareFolder folder = new("// test firmware");
        FirmwareInfoService service = new(folder.Path);

        FirmwareSourceReadResult result = await service.ReadSketchAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(System.IO.Path.Combine(folder.Path, FirmwareInfoService.SketchFileName), result.FirmwareFilePath);
        Assert.Contains("test firmware", result.SourceText);
    }

    [Fact]
    public async Task ReadSketchAsync_ReturnsClearMessageWhenFileIsMissing()
    {
        string folderPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"missing-firmware-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folderPath);
        try
        {
            FirmwareInfoService service = new(folderPath);

            FirmwareSourceReadResult result = await service.ReadSketchAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("Firmware file not found", result.Message);
            Assert.Empty(result.SourceText);
        }
        finally
        {
            Directory.Delete(folderPath, recursive: true);
        }
    }

    private sealed class TemporaryFirmwareFolder : IDisposable
    {
        public TemporaryFirmwareFolder(string sourceText)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"firmware-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
            File.WriteAllText(System.IO.Path.Combine(Path, FirmwareInfoService.SketchFileName), sourceText);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
