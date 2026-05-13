namespace BiomedicalSignalPlotter.Serial;

public sealed record SerialPortSelectionResult(
    IReadOnlyList<SerialPortDisplayInfo> Ports,
    SerialPortDisplayInfo? SelectedPort,
    string? Message);
