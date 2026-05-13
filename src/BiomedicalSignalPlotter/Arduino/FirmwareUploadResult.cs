namespace BiomedicalSignalPlotter.Arduino;

public sealed record FirmwareUploadResult(bool Succeeded, string Message, string? PortName = null);
