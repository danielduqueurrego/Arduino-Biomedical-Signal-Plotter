using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Serial;

public interface ISerialService : IAsyncDisposable
{
    event EventHandler<SignalSample>? SampleReceived;
    event EventHandler<string>? StatusChanged;
    event EventHandler<string>? MetadataReceived;

    bool IsConnected { get; }

    string? PortName { get; }

    int ExpectedChannelCount { get; set; }

    string[] GetAvailablePorts();

    void Connect(string portName, int baudRate = 115_200);

    Task SendLineAsync(string line, CancellationToken cancellationToken = default);

    Task DisconnectAsync();
}
