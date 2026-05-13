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
        : this(new ProcessCommandRunner(), FirmwareInfoService.ResolveDefaultSketchFolderPath())
    {
    }

    public FirmwareUploadService(ICommandRunner commandRunner, string sketchPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sketchPath);

        _commandRunner = commandRunner;
        _sketchPath = sketchPath;
        LastUploadLog = FirmwareUploadLog.Empty(_sketchPath);
    }

    public string SketchPath => _sketchPath;

    public FirmwareUploadLog LastUploadLog { get; private set; }

    public async Task<FirmwareUploadResult> UploadUnoR4WifiAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<FirmwareUploadCommandLog> commandLogs = [];
        DateTimeOffset startedAt = DateTimeOffset.Now;

        try
        {
            return await UploadUnoR4WifiCoreAsync(progress, commandLogs, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            LastUploadLog = new FirmwareUploadLog(
                _sketchPath,
                startedAt,
                DateTimeOffset.Now,
                commandLogs.ToArray());
        }
    }

    public static IReadOnlyList<string> BuildCompileArguments(string fqbn, string sketchPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fqbn);
        ArgumentException.ThrowIfNullOrWhiteSpace(sketchPath);

        return ["compile", "--fqbn", fqbn, sketchPath];
    }

    public static IReadOnlyList<string> BuildUploadArguments(string portName, string fqbn, string sketchPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fqbn);
        ArgumentException.ThrowIfNullOrWhiteSpace(sketchPath);

        return ["upload", "-p", portName, "--fqbn", fqbn, sketchPath];
    }

    public static string FormatCommand(string fileName, IReadOnlyList<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return string.Join(' ', [fileName, .. arguments.Select(QuoteArgument)]);
    }

    private async Task<FirmwareUploadResult> UploadUnoR4WifiCoreAsync(
        IProgress<string>? progress,
        List<FirmwareUploadCommandLog> commandLogs,
        CancellationToken cancellationToken)
    {
        progress?.Report("Checking Arduino CLI...");
        CommandResult versionResult;
        try
        {
            versionResult = await RunLoggedAsync(
                commandLogs,
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
        CommandResult boardListResult = await RunLoggedAsync(
            commandLogs,
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
        CommandResult compileResult = await RunLoggedAsync(
            commandLogs,
            "arduino-cli",
            BuildCompileArguments(fqbn, _sketchPath),
            CompileTimeout,
            cancellationToken).ConfigureAwait(false);

        if (compileResult.ExitCode != 0)
        {
            return new FirmwareUploadResult(false, $"Upload failed while compiling firmware: {GetCommandOutput(compileResult)}", board.PortName);
        }

        progress?.Report("Uploading firmware...");
        CommandResult uploadResult = await RunLoggedAsync(
            commandLogs,
            "arduino-cli",
            BuildUploadArguments(board.PortName, fqbn, _sketchPath),
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

    private async Task<CommandResult> RunLoggedAsync(
        List<FirmwareUploadCommandLog> commandLogs,
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        string commandText = FormatCommand(fileName, arguments);

        try
        {
            CommandResult result = await _commandRunner.RunAsync(
                fileName,
                arguments,
                timeout,
                cancellationToken).ConfigureAwait(false);

            commandLogs.Add(new FirmwareUploadCommandLog(
                commandText,
                result.ExitCode,
                result.StandardOutput,
                result.StandardError));

            return result;
        }
        catch (Exception ex)
        {
            commandLogs.Add(new FirmwareUploadCommandLog(
                commandText,
                null,
                string.Empty,
                ex.Message));
            throw;
        }
    }

    private static string QuoteArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return "\"\"";
        }

        return argument.Any(char.IsWhiteSpace) || argument.Contains('"', StringComparison.Ordinal)
            ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : argument;
    }
}
