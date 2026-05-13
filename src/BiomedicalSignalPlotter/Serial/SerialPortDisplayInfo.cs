namespace BiomedicalSignalPlotter.Serial;

public sealed record SerialPortDisplayInfo(
    string PortName,
    string? BoardName,
    string? Fqbn,
    bool IsArduinoUnoR4Wifi)
{
    public string DisplayName => string.IsNullOrWhiteSpace(BoardName)
        ? PortName
        : $"{PortName} — {BoardName}";

    public override string ToString()
    {
        return DisplayName;
    }
}
