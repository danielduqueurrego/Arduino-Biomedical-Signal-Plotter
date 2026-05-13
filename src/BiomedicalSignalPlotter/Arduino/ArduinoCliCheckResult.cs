namespace BiomedicalSignalPlotter.Arduino;

public sealed record ArduinoCliCheckResult(bool IsAvailable, string Message, string? VersionText);
