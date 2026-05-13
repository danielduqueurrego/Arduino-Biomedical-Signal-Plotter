namespace BiomedicalSignalPlotter.Arduino;

public sealed class FirmwareInfoService
{
    public const string SketchFileName = "TwoChannelCsvStreamer.ino";

    public static readonly string RelativeSketchFolderPath = Path.Combine(
        "firmware",
        "arduino",
        "TwoChannelCsvStreamer");

    public FirmwareInfoService()
        : this(ResolveDefaultSketchFolderPath())
    {
    }

    public FirmwareInfoService(string sketchFolderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sketchFolderPath);
        SketchFolderPath = Path.GetFullPath(sketchFolderPath);
    }

    public string SketchFolderPath { get; }

    public string SketchFilePath => Path.Combine(SketchFolderPath, SketchFileName);

    public async Task<FirmwareSourceReadResult> ReadSketchAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(SketchFilePath))
        {
            return new FirmwareSourceReadResult(
                false,
                SketchFilePath,
                string.Empty,
                $"Firmware file not found: {SketchFilePath}");
        }

        try
        {
            string sourceText = await File.ReadAllTextAsync(SketchFilePath, cancellationToken).ConfigureAwait(false);
            return new FirmwareSourceReadResult(
                true,
                SketchFilePath,
                sourceText,
                $"Loaded firmware file: {SketchFilePath}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new FirmwareSourceReadResult(
                false,
                SketchFilePath,
                string.Empty,
                $"Unable to read firmware file: {ex.Message}");
        }
    }

    public static string ResolveDefaultSketchFolderPath()
    {
        string[] roots =
        [
            Environment.CurrentDirectory,
            AppContext.BaseDirectory
        ];

        foreach (string root in roots)
        {
            DirectoryInfo? directory = new(root);
            while (directory is not null)
            {
                string candidate = Path.GetFullPath(Path.Combine(directory.FullName, RelativeSketchFolderPath));
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        return Path.GetFullPath(RelativeSketchFolderPath);
    }
}
