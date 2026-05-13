using System.ComponentModel;
using System.Text.Json;

namespace BiomedicalSignalPlotter.Arduino;

public sealed class ArduinoCliService
{
    private static readonly TimeSpan VersionCheckTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan BoardListTimeout = TimeSpan.FromSeconds(20);
    private readonly ICommandRunner _commandRunner;

    public ArduinoCliService()
        : this(new ProcessCommandRunner())
    {
    }

    public ArduinoCliService(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<ArduinoCliCheckResult> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            CommandResult result = await _commandRunner.RunAsync(
                "arduino-cli",
                ["version"],
                VersionCheckTimeout,
                cancellationToken).ConfigureAwait(false);

            string output = string.IsNullOrWhiteSpace(result.StandardOutput)
                ? result.StandardError.Trim()
                : result.StandardOutput.Trim();

            if (result.ExitCode == 0)
            {
                string versionText = FirstNonEmptyLine(output);
                return new ArduinoCliCheckResult(true, $"Arduino CLI available: {versionText}", output);
            }

            return new ArduinoCliCheckResult(
                false,
                $"Arduino CLI check failed: {FirstNonEmptyLine(output)}",
                output);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            return new ArduinoCliCheckResult(
                false,
                "Arduino CLI is required for firmware upload/setup, but not for simulation or plotting from an already programmed board.",
                null);
        }
        catch (TimeoutException ex)
        {
            return new ArduinoCliCheckResult(false, ex.Message, null);
        }
    }

    public async Task<IReadOnlyList<ArduinoBoardInfo>> ListBoardsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            CommandResult result = await _commandRunner.RunAsync(
                "arduino-cli",
                ["board", "list", "--json"],
                BoardListTimeout,
                cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return [];
            }

            return ParseBoardList(result.StandardOutput);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or TimeoutException or JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<ArduinoBoardInfo> ParseBoardList(string boardListJson)
    {
        using JsonDocument document = JsonDocument.Parse(boardListJson);

        if (!document.RootElement.TryGetProperty("detected_ports", out JsonElement detectedPorts) ||
            detectedPorts.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<ArduinoBoardInfo> boards = [];
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
                if (!string.IsNullOrWhiteSpace(fqbn) || !string.IsNullOrWhiteSpace(name))
                {
                    boards.Add(new ArduinoBoardInfo(portName, fqbn, name));
                    break;
                }
            }
        }

        return boards;
    }

    private static string FirstNonEmptyLine(string text)
    {
        return text
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "no version output";
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
