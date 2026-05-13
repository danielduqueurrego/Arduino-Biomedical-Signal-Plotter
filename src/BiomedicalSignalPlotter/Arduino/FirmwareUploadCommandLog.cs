namespace BiomedicalSignalPlotter.Arduino;

public sealed record FirmwareUploadCommandLog(
    string CommandText,
    int? ExitCode,
    string StandardOutput,
    string StandardError);
