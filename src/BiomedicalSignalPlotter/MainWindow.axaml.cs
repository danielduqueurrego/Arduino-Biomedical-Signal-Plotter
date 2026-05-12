using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BiomedicalSignalPlotter.Models;
using BiomedicalSignalPlotter.Recording;
using BiomedicalSignalPlotter.Serial;
using BiomedicalSignalPlotter.Services;

namespace BiomedicalSignalPlotter;

public partial class MainWindow : Window
{
    private const int BufferCapacity = 2_000;
    private readonly CircularSignalBuffer _signalBuffer = new(BufferCapacity);
    private readonly SimulatedDataService _simulatedDataService = new();
    private readonly ISerialService _serialService = new SerialService();
    private readonly RecordingService _recordingService = new();
    private readonly DispatcherTimer _plotRefreshTimer;
    private string _plotTitle = "Simulated two-channel data";

    public MainWindow()
    {
        InitializeComponent();

        ConfigurePlot();
        SignalPlot.Refresh();
        _simulatedDataService.SampleGenerated += SimulatedDataService_SampleGenerated;
        _serialService.SampleReceived += SerialService_SampleReceived;
        _serialService.StatusChanged += SerialService_StatusChanged;
        RefreshSerialPorts(updateStatus: false);

        _plotRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _plotRefreshTimer.Tick += PlotRefreshTimer_Tick;
        _plotRefreshTimer.Start();
        StartSimulation();
        UpdateRecordingUi();

        Closed += MainWindow_Closed;
    }

    private void ConfigurePlot()
    {
        SignalPlot.Plot.Title(_plotTitle);
        SignalPlot.Plot.XLabel("Time (s)");
        SignalPlot.Plot.YLabel("ADC counts");
        SignalPlot.Plot.ShowLegend();
    }

    private void SimulatedDataService_SampleGenerated(object? sender, SignalSample sample)
    {
        _signalBuffer.Add(sample);
        _recordingService.Record(sample, RecordedSampleSource.Simulated);
    }

    private void SerialService_SampleReceived(object? sender, SignalSample sample)
    {
        _signalBuffer.Add(sample);
        _recordingService.Record(sample, RecordedSampleSource.Serial);
    }

    private void SerialService_StatusChanged(object? sender, string status)
    {
        Dispatcher.UIThread.Post(() => StatusText.Text = status);
    }

    private void PlotRefreshTimer_Tick(object? sender, EventArgs e)
    {
        SignalSnapshot snapshot = _signalBuffer.Snapshot();

        SignalPlot.Plot.Clear();
        ConfigurePlot();

        if (snapshot.Count > 1)
        {
            var channel1 = SignalPlot.Plot.Add.Scatter(snapshot.TimeSeconds, snapshot.Channel1);
            channel1.LegendText = "Channel 1 - ECG placeholder";

            var channel2 = SignalPlot.Plot.Add.Scatter(snapshot.TimeSeconds, snapshot.Channel2);
            channel2.LegendText = "Channel 2 - pressure/PPG placeholder";

            SignalPlot.Plot.Axes.AutoScale();
        }

        SignalPlot.Refresh();
        UpdateRecordingUi();
    }

    private void ConnectButton_Click(object? sender, RoutedEventArgs e)
    {
        _ = ToggleSerialConnectionAsync();
    }

    private async void SimulationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_simulatedDataService.IsRunning)
        {
            await StopSimulationAsync();
            return;
        }

        if (_serialService.IsConnected)
        {
            await DisconnectSerialAsync();
        }

        StartSimulation();
    }

    private void StartSimulation()
    {
        _signalBuffer.Clear();
        _plotTitle = "Simulated two-channel data";
        _simulatedDataService.Start();
        SimulationButton.Content = "Stop Simulation";
        StatusText.Text = "Simulation running at 200 Hz. Plot refreshes at approximately 30 Hz.";
    }

    private async Task StopSimulationAsync()
    {
        await _simulatedDataService.StopAsync();
        SimulationButton.Content = "Start Simulation";
        StatusText.Text = $"Simulation stopped. Buffer holds {_signalBuffer.Count} samples.";
    }

    private void RefreshPortsButton_Click(object? sender, RoutedEventArgs e)
    {
        RefreshSerialPorts(updateStatus: true);
    }

    private void RefreshSerialPorts(bool updateStatus)
    {
        string[] ports = _serialService.GetAvailablePorts();
        SerialPortComboBox.ItemsSource = ports;
        SerialPortComboBox.SelectedIndex = ports.Length > 0 ? 0 : -1;

        if (updateStatus)
        {
            StatusText.Text = ports.Length == 0
                ? "No serial ports found."
                : $"Found {ports.Length} serial port(s): {string.Join(", ", ports)}.";
        }
    }

    private async Task ToggleSerialConnectionAsync()
    {
        if (_serialService.IsConnected)
        {
            await DisconnectSerialAsync();
            return;
        }

        if (SerialPortComboBox.SelectedItem is not string portName || string.IsNullOrWhiteSpace(portName))
        {
            StatusText.Text = "Select a serial port before connecting.";
            return;
        }

        try
        {
            if (_simulatedDataService.IsRunning)
            {
                await StopSimulationAsync();
            }

            _signalBuffer.Clear();
            _plotTitle = "Serial two-channel data";
            _serialService.Connect(portName);
            ConnectButton.Content = "Disconnect";
            SimulationButton.Content = "Start Simulation";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ConnectButton.Content = "Connect";
            StatusText.Text = $"Unable to connect to {portName}: {ex.Message}";
        }
    }

    private async Task DisconnectSerialAsync()
    {
        await _serialService.DisconnectAsync();
        ConnectButton.Content = "Connect";
    }

    private void StartRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        _recordingService.Start();
        RecordingStatusText.Text = "Recording";
        UpdateRecordingUi();
    }

    private void StopRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        _recordingService.Stop();
        RecordingStatusText.Text = "Recording stopped";
        UpdateRecordingUi();
    }

    private async void SaveRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<RecordedSample> samples = _recordingService.Snapshot();
        if (samples.Count == 0)
        {
            RecordingStatusText.Text = "No recorded samples to save";
            UpdateRecordingUi();
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider.CanSave != true)
        {
            RecordingStatusText.Text = "CSV save dialog is not available";
            UpdateRecordingUi();
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Recorded CSV",
            SuggestedFileName = $"biomedical-recording-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            DefaultExtension = "csv",
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType("CSV Files")
                {
                    Patterns = ["*.csv"],
                    MimeTypes = ["text/csv"]
                }
            ]
        });

        if (file is null)
        {
            RecordingStatusText.Text = "Save canceled";
            UpdateRecordingUi();
            return;
        }

        try
        {
            RecordingStatusText.Text = "Saving CSV...";
            await using Stream stream = await file.OpenWriteAsync();
            await CsvRecordingExporter.WriteAsync(stream, samples);
            RecordingStatusText.Text = $"Saved {samples.Count} samples";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RecordingStatusText.Text = $"Save failed: {ex.Message}";
        }

        UpdateRecordingUi();
    }

    private void ClearRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        _recordingService.Clear();
        RecordingStatusText.Text = _recordingService.IsRecording ? "Recording cleared and running" : "Recording cleared";
        UpdateRecordingUi();
    }

    private void UpdateRecordingUi()
    {
        int count = _recordingService.Count;
        RecordedSampleCountText.Text = count == 1 ? "1 sample" : $"{count} samples";
        StartRecordingButton.IsEnabled = !_recordingService.IsRecording;
        StopRecordingButton.IsEnabled = _recordingService.IsRecording;
        SaveRecordingButton.IsEnabled = count > 0;
        ClearRecordingButton.IsEnabled = count > 0 || _recordingService.IsRecording;
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _plotRefreshTimer.Stop();
        await _serialService.DisposeAsync();
        await _simulatedDataService.StopAsync();
    }
}
