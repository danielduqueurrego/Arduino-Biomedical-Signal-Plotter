using System.Diagnostics;
using System.IO.Ports;
using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Serial;

public sealed class SerialService : ISerialService
{
    private readonly object _syncRoot = new();
    private readonly SerialLineParser _parser = new();
    private readonly Stopwatch _stopwatch = new();
    private SerialPort? _serialPort;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readTask;
    private int _expectedChannelCount = AnalogChannelLimits.Default;

    public event EventHandler<SignalSample>? SampleReceived;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? MetadataReceived;

    public bool IsConnected { get; private set; }

    public string? PortName { get; private set; }

    public int ExpectedChannelCount
    {
        get => Volatile.Read(ref _expectedChannelCount);
        set => Volatile.Write(ref _expectedChannelCount, AnalogChannelLimits.Validate(value));
    }

    public string[] GetAvailablePorts()
    {
        try
        {
            return SerialPort.GetPortNames()
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusChanged?.Invoke(this, $"Unable to list serial ports: {ex.Message}");
            return [];
        }
    }

    public void Connect(string portName, int baudRate = 115_200)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);

        lock (_syncRoot)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("A serial port is already connected.");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _serialPort = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 500,
                DtrEnable = true,
                RtsEnable = true
            };

            try
            {
                _serialPort.Open();
            }
            catch
            {
                _serialPort.Dispose();
                _serialPort = null;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                throw;
            }

            _stopwatch.Restart();
            IsConnected = true;
            PortName = portName;
            _readTask = Task.Run(() => ReadLoop(_serialPort, _cancellationTokenSource.Token));
        }

        StatusChanged?.Invoke(this, $"Connected to {portName} at {baudRate} baud.");
    }

    public Task SendLineAsync(string line, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);

        SerialPort serialPort;
        lock (_syncRoot)
        {
            if (!IsConnected || _serialPort is null)
            {
                throw new InvalidOperationException("Connect to Arduino before applying device settings.");
            }

            serialPort = _serialPort;
        }

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                serialPort.WriteLine(line);
            },
            cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        SerialPort? serialPort;
        CancellationTokenSource? cancellationTokenSource;
        Task? readTask;
        string? disconnectedPort;

        lock (_syncRoot)
        {
            serialPort = _serialPort;
            cancellationTokenSource = _cancellationTokenSource;
            readTask = _readTask;
            disconnectedPort = PortName;

            _serialPort = null;
            _cancellationTokenSource = null;
            _readTask = null;
            IsConnected = false;
            PortName = null;
            _stopwatch.Reset();
        }

        if (serialPort is null)
        {
            return;
        }

        cancellationTokenSource?.Cancel();

        try
        {
            serialPort.Close();
        }
        catch (IOException)
        {
        }

        if (readTask is not null)
        {
            try
            {
                await readTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        serialPort.Dispose();
        cancellationTokenSource?.Dispose();

        if (!string.IsNullOrWhiteSpace(disconnectedPort))
        {
            StatusChanged?.Invoke(this, $"Disconnected from {disconnectedPort}.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }

    private void ReadLoop(SerialPort serialPort, CancellationToken cancellationToken)
    {
        int rejectedDataLineCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string line = serialPort.ReadLine();
                if (SerialLineParser.IsCommentOrMetadataLine(line))
                {
                    MetadataReceived?.Invoke(this, line.Trim());
                    continue;
                }

                if (_parser.TryParse(line, ExpectedChannelCount, out SerialChannelValues? values))
                {
                    rejectedDataLineCount = 0;
                    SignalSample sample = new(
                        _stopwatch.Elapsed.TotalSeconds,
                        values.Values);

                    SampleReceived?.Invoke(this, sample);
                }
                else
                {
                    rejectedDataLineCount++;
                    if (rejectedDataLineCount % 100 == 0)
                    {
                        StatusChanged?.Invoke(
                            this,
                            $"Ignored {rejectedDataLineCount} serial data rows. Check channel count and firmware settings.");
                    }
                }
            }
            catch (TimeoutException)
            {
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    StatusChanged?.Invoke(this, $"Serial read error: {ex.Message}");
                }

                return;
            }
        }
    }
}
