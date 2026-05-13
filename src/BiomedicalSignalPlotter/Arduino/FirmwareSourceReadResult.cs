namespace BiomedicalSignalPlotter.Arduino;

public sealed record FirmwareSourceReadResult(
    bool Succeeded,
    string FirmwareFilePath,
    string SourceText,
    string Message);
