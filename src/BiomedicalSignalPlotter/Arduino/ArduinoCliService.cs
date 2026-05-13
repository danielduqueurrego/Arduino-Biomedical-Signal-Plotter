using System.ComponentModel;

namespace BiomedicalSignalPlotter.Arduino;

public sealed class ArduinoCliService
{
    private static readonly TimeSpan VersionCheckTimeout = TimeSpan.FromSeconds(10);
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

    private static string FirstNonEmptyLine(string text)
    {
        return text
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "no version output";
    }
}
