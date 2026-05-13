using BiomedicalSignalPlotter.Arduino;
using BiomedicalSignalPlotter.Serial;

namespace BiomedicalSignalPlotter.Tests;

public class SerialPortSelectionServiceTests
{
    [Fact]
    public void CreateDisplayPorts_FormatsArduinoBoardName()
    {
        IReadOnlyList<SerialPortDisplayInfo> ports = SerialPortSelectionService.CreateDisplayPorts(
            ["COM5"],
            [UnoBoard("COM5")]);

        SerialPortDisplayInfo port = Assert.Single(ports);
        Assert.Equal("COM5", port.PortName);
        Assert.Equal(FirmwareUploadService.UnoR4WifiBoardName, port.BoardName);
        Assert.Equal(FirmwareUploadService.UnoR4WifiFqbn, port.Fqbn);
        Assert.True(port.IsArduinoUnoR4Wifi);
        Assert.Equal("COM5 — Arduino UNO R4 WiFi", port.DisplayName);
        Assert.Equal(port.DisplayName, port.ToString());
    }

    [Fact]
    public void CreateDisplayPorts_FallsBackToPortNameWhenBoardInfoIsUnavailable()
    {
        IReadOnlyList<SerialPortDisplayInfo> ports = SerialPortSelectionService.CreateDisplayPorts(
            ["COM1"],
            []);

        SerialPortDisplayInfo port = Assert.Single(ports);
        Assert.Equal("COM1", port.PortName);
        Assert.Null(port.BoardName);
        Assert.Equal("COM1", port.DisplayName);
        Assert.False(port.IsArduinoUnoR4Wifi);
    }

    [Fact]
    public void ChoosePort_PreservesPreferredPortByName()
    {
        IReadOnlyList<SerialPortDisplayInfo> ports = SerialPortSelectionService.CreateDisplayPorts(
            ["COM1", "COM5"],
            [UnoBoard("COM5")]);

        SerialPortSelectionResult result = SerialPortSelectionService.ChoosePort(ports, "COM5");

        Assert.Equal("COM5", result.SelectedPort?.PortName);
        Assert.Null(result.Message);
    }

    [Fact]
    public void ChoosePort_PreservesPreferredPortAfterUploadWhenStillPresent()
    {
        IReadOnlyList<SerialPortDisplayInfo> ports = SerialPortSelectionService.CreateDisplayPorts(
            ["COM5", "COM7"],
            [UnoBoard("COM7")]);

        SerialPortSelectionResult result = SerialPortSelectionService.ChoosePort(
            ports,
            preferredPortName: "COM5",
            fallbackPortName: "COM7");

        Assert.Equal("COM5", result.SelectedPort?.PortName);
        Assert.Null(result.Message);
    }

    [Fact]
    public void ChoosePort_UsesUploadedPortWhenPreferredPortIsGone()
    {
        IReadOnlyList<SerialPortDisplayInfo> ports = SerialPortSelectionService.CreateDisplayPorts(
            ["COM7"],
            [UnoBoard("COM7")]);

        SerialPortSelectionResult result = SerialPortSelectionService.ChoosePort(
            ports,
            preferredPortName: "COM5",
            fallbackPortName: "COM7");

        Assert.Equal("COM7", result.SelectedPort?.PortName);
        Assert.Contains("Previously selected port COM5", result.Message);
    }

    [Fact]
    public void ChoosePort_SelectsExactlyOneUnoWhenPreferredPortIsGone()
    {
        IReadOnlyList<SerialPortDisplayInfo> ports = SerialPortSelectionService.CreateDisplayPorts(
            ["COM1", "COM6"],
            [UnoBoard("COM6")]);

        SerialPortSelectionResult result = SerialPortSelectionService.ChoosePort(ports, "COM5");

        Assert.Equal("COM6", result.SelectedPort?.PortName);
        Assert.Contains("selected detected Arduino UNO R4 WiFi", result.Message);
    }

    [Fact]
    public void ChoosePort_DoesNotAutoSelectWhenMultipleUnoBoardsExist()
    {
        IReadOnlyList<SerialPortDisplayInfo> ports = SerialPortSelectionService.CreateDisplayPorts(
            ["COM5", "COM6"],
            [UnoBoard("COM5"), UnoBoard("COM6")]);

        SerialPortSelectionResult result = SerialPortSelectionService.ChoosePort(ports, "COM7");

        Assert.Null(result.SelectedPort);
        Assert.Contains("multiple Arduino UNO R4 WiFi boards", result.Message);
    }

    private static ArduinoBoardInfo UnoBoard(string portName)
    {
        return new ArduinoBoardInfo(
            portName,
            FirmwareUploadService.UnoR4WifiFqbn,
            FirmwareUploadService.UnoR4WifiBoardName);
    }
}
