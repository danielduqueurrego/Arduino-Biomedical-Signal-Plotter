using BiomedicalSignalPlotter.Arduino;

namespace BiomedicalSignalPlotter.Serial;

public static class SerialPortSelectionService
{
    public static IReadOnlyList<SerialPortDisplayInfo> CreateDisplayPorts(
        IEnumerable<string> portNames,
        IEnumerable<ArduinoBoardInfo> boards)
    {
        Dictionary<string, ArduinoBoardInfo> boardByPort = boards
            .Where(board => !string.IsNullOrWhiteSpace(board.PortName))
            .GroupBy(board => board.PortName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        return portNames
            .Where(portName => !string.IsNullOrWhiteSpace(portName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(portName =>
            {
                if (!boardByPort.TryGetValue(portName, out ArduinoBoardInfo? board))
                {
                    return new SerialPortDisplayInfo(portName, null, null, false);
                }

                return new SerialPortDisplayInfo(
                    portName,
                    NullIfWhiteSpace(board.Name),
                    NullIfWhiteSpace(board.Fqbn),
                    IsUnoR4Wifi(board));
            })
            .ToArray();
    }

    public static SerialPortSelectionResult ChoosePort(
        IReadOnlyList<SerialPortDisplayInfo> ports,
        string? preferredPortName,
        string? fallbackPortName = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredPortName))
        {
            SerialPortDisplayInfo? preferredPort = FindPort(ports, preferredPortName);
            if (preferredPort is not null)
            {
                return new SerialPortSelectionResult(ports, preferredPort, null);
            }

            SerialPortDisplayInfo? fallbackPort = FindPort(ports, fallbackPortName);
            if (fallbackPort is not null)
            {
                return new SerialPortSelectionResult(
                    ports,
                    fallbackPort,
                    $"Previously selected port {preferredPortName} was not found; selected uploaded Arduino on {fallbackPort.PortName}.");
            }

            SerialPortDisplayInfo[] unoBoards = ports
                .Where(port => port.IsArduinoUnoR4Wifi)
                .ToArray();

            if (unoBoards.Length == 1)
            {
                return new SerialPortSelectionResult(
                    ports,
                    unoBoards[0],
                    $"Previously selected port {preferredPortName} was not found; selected detected Arduino UNO R4 WiFi on {unoBoards[0].PortName}.");
            }

            if (unoBoards.Length > 1)
            {
                return new SerialPortSelectionResult(
                    ports,
                    null,
                    $"Previously selected port {preferredPortName} was not found, and multiple Arduino UNO R4 WiFi boards were detected. Select a serial port.");
            }

            string message = ports.Count == 0
                ? $"Previously selected port {preferredPortName} was not found. No serial ports are available."
                : $"Previously selected port {preferredPortName} was not found. Select a serial port.";

            return new SerialPortSelectionResult(ports, null, message);
        }

        SerialPortDisplayInfo[] detectedUnoBoards = ports
            .Where(port => port.IsArduinoUnoR4Wifi)
            .ToArray();

        if (detectedUnoBoards.Length == 1)
        {
            return new SerialPortSelectionResult(
                ports,
                detectedUnoBoards[0],
                $"Selected detected Arduino UNO R4 WiFi on {detectedUnoBoards[0].PortName}.");
        }

        if (detectedUnoBoards.Length > 1)
        {
            return new SerialPortSelectionResult(
                ports,
                null,
                "Multiple Arduino UNO R4 WiFi boards were detected. Select a serial port.");
        }

        string defaultMessage = ports.Count == 0
            ? "No serial ports found."
            : $"Found {ports.Count} serial port(s). Select a serial port.";

        return new SerialPortSelectionResult(ports, null, defaultMessage);
    }

    public static bool IsUnoR4Wifi(ArduinoBoardInfo board)
    {
        return string.Equals(board.Fqbn, FirmwareUploadService.UnoR4WifiFqbn, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(board.Name, FirmwareUploadService.UnoR4WifiBoardName, StringComparison.OrdinalIgnoreCase);
    }

    private static SerialPortDisplayInfo? FindPort(
        IEnumerable<SerialPortDisplayInfo> ports,
        string? portName)
    {
        return string.IsNullOrWhiteSpace(portName)
            ? null
            : ports.FirstOrDefault(port => string.Equals(port.PortName, portName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
