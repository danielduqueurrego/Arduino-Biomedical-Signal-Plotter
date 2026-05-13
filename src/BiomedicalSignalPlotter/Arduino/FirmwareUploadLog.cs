using System.Text;

namespace BiomedicalSignalPlotter.Arduino;

public sealed record FirmwareUploadLog(
    string SketchPath,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<FirmwareUploadCommandLog> Commands)
{
    public static FirmwareUploadLog Empty(string sketchPath)
    {
        return new FirmwareUploadLog(sketchPath, DateTimeOffset.MinValue, null, []);
    }

    public string FormatForDisplay()
    {
        StringBuilder builder = new();
        builder.AppendLine($"Sketch folder: {SketchPath}");

        if (Commands.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("No firmware upload has been attempted yet.");
            return builder.ToString();
        }

        builder.AppendLine($"Started: {StartedAt:yyyy-MM-dd HH:mm:ss zzz}");
        if (CompletedAt is not null)
        {
            builder.AppendLine($"Completed: {CompletedAt:yyyy-MM-dd HH:mm:ss zzz}");
        }

        for (int i = 0; i < Commands.Count; i++)
        {
            FirmwareUploadCommandLog command = Commands[i];
            builder.AppendLine();
            builder.AppendLine($"Command {i + 1}: {command.CommandText}");
            builder.AppendLine(command.ExitCode is null
                ? "Exit code: unavailable"
                : $"Exit code: {command.ExitCode}");

            if (!string.IsNullOrWhiteSpace(command.StandardOutput))
            {
                builder.AppendLine("stdout:");
                builder.AppendLine(command.StandardOutput.Trim());
            }

            if (!string.IsNullOrWhiteSpace(command.StandardError))
            {
                builder.AppendLine("stderr:");
                builder.AppendLine(command.StandardError.Trim());
            }
        }

        return builder.ToString();
    }
}
