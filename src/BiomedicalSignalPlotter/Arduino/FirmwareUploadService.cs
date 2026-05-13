using System.ComponentModel;
using System.Text.Json;

namespace BiomedicalSignalPlotter.Arduino;

public sealed class FirmwareUploadService
{
    public const string UnoR4WifiFqbn = "arduino:renesas_uno:unor4wifi";
    public const string UnoR4WifiBoardName = "Arduino UNO R4 WiFi";

    private static readonly TimeSpan VersionCheckTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan BoardListTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan CompileTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan UploadTimeout = TimeSpan.FromMinutes(2);

    private readonly ICommandRunner _commandRunner;
    private readonly string _sketchPath;

    public FirmwareUploadService()
        : this(new ProcessCommandRunner(), ResolveDefaultSketchPath())
    {
    }

    public FirmwareUploadService(ICommandRunner commandRunner, string sketchPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sketchPath);

        _commandRunner = commandRunner;
        _sketchPath = sketchPath;
    }

    public async Task<FirmwareUploadResult> UploadUnoR4WifiAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Checking Arduino CLI...");
        CommandResult versionResult;
        try
        {
            versionResult = await _commandRunner.RunAsync(
                "arduino-cli",
                ["version"],
                VersionCheckTimeout,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            return new FirmwareUploadResult(
                false,
                "Arduino CLI not found. Arduino CLI is required for firmware upload, but not for plotting from an already programmed board.");
        }
        catch (TimeoutException ex)
        {
            return new FirmwareUploadResult(false, $"Arduino CLI check failed: {ex.Message}");
        }

        if (versionResult.ExitCode != 0)
        {
            return new FirmwareUploadResult(false, $"Arduino CLI check failed: {GetCommandOutput(versionResult)}");
        }

        progress?.Report("Detecting connected boards...");
        CommandResult boardListResult = await _commandRunner.RunAsync(
            "arduino-cli",
            ["board", "list", "--json"],
            BoardListTimeout,
            cancellationToken).ConfigureAwait(false);

        if (boardListResult.ExitCode != 0)
        {
            return new FirmwareUploadResult(false, $"Board detection failed: {GetCommandOutput(boardListResult)}");
        }

        IReadOnlyList<ArduinoBoardInfo> matches;
        try
        {
            matches = FindUnoR4WifiBoards(boardListResult.StandardOutput);
        }
        catch (JsonException ex)
        {
            return new FirmwareUploadResult(false, $"Board detection failed: unable to parse Arduino CLI output. {ex.Message}");
        }

        if (matches.Count == 0)
        {
            return new FirmwareUploadResult(false, "No UNO R4 WiFi detected. Connect the board by USB and run Refresh Ports or Check Arduino CLI again.");
        }

        if (matches.Count > 1)
        {
            string ports = string.Join(", ", matches.Select(board => $"{board.PortName} [{board.Fqbn}]"));
            return new FirmwareUploadResult(false, $"Multiple UNO R4 WiFi boards detected: {ports}. Disconnect extras and try again.");
        }

        ArduinoBoardInfo board = matches[0];
        string fqbn = string.IsNullOrWhiteSpace(board.Fqbn) ? UnoR4WifiFqbn : board.Fqbn;

        progress?.Report("Compiling firmware...");
        CommandResult compileResult = await _commandRunner.RunAsync(
            "arduino-cli",
            ["compile", "--fqbn", fqbn, _sketchPath],
            CompileTimeout,
            cancellationToken).ConfigureAwait(false);

        if (compileResult.ExitCode != 0)
        {
            return new FirmwareUploadResult(false, $"Upload failed while compiling firmware: {GetCommandOutput(compileResult)}", board.PortName);
        }

        progress?.Report("Uploading firmware...");
        CommandResult uploadResult = await _commandRunner.RunAsync(
            "arduino-cli",
            ["upload", "-p", board.PortName, "--fqbn", fqbn, _sketchPath],
            UploadTimeout,
            cancellationToken).ConfigureAwait(false);

        if (uploadResult.ExitCode != 0)
        {
            return new FirmwareUploadResult(false, $"Upload failed: {GetCommandOutput(uploadResult)}", board.PortName);
        }

        return new FirmwareUploadResult(true, $"Upload succeeded on {board.PortName}.", board.PortName);
    }

    public static IReadOnlyList<ArduinoBoardInfo> FindUnoR4WifiBoards(string boardListJson)
    {
        using JsonDocument document = JsonDocument.Parse(boardListJson);

        if (!document.RootElement.TryGetProperty("detected_ports", out JsonElement detectedPorts) ||
            detectedPorts.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<ArduinoBoardInfo> matches = [];
        foreach (JsonElement detectedPort in detectedPorts.EnumerateArray())
        {
            string portName = ReadNestedString(detectedPort, "port", "address");
            if (string.IsNullOrWhiteSpace(portName) ||
                !detectedPort.TryGetProperty("matching_boards", out JsonElement matchingBoards) ||
                matchingBoards.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (JsonElement board in matchingBoards.EnumerateArray())
            {
                string fqbn = ReadString(board, "fqbn");
                string name = ReadString(board, "name");

                if (string.Equals(fqbn, UnoR4WifiFqbn, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, UnoR4WifiBoardName, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(new ArduinoBoardInfo(portName, fqbn, name));
                }
            }
        }

        return matches;
    }

    private static string ResolveDefaultSketchPath()
    {
        string relativePath = Path.Combine("firmware", "arduino", "TwoChannelCsvStreamer");
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
                string candidate = Path.GetFullPath(Path.Combine(directory.FullName, relativePath));
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        return relativePath;
    }

    private static string GetCommandOutput(CommandResult result)
    {
        string output = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput.Trim()
            : result.StandardError.Trim();

        return string.IsNullOrWhiteSpace(output) ? $"arduino-cli exited with code {result.ExitCode}." : output;
    }

    private static string ReadNestedString(JsonElement element, string parentName, string childName)
    {
        return element.TryGetProperty(parentName, out JsonElement parent)
            ? ReadString(parent, childName)
            : string.Empty;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }
}
