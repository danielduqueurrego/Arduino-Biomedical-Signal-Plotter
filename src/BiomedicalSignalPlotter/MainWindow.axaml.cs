using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BiomedicalSignalPlotter.Arduino;
using BiomedicalSignalPlotter.Configuration;
using BiomedicalSignalPlotter.Models;
using BiomedicalSignalPlotter.Recording;
using BiomedicalSignalPlotter.Serial;
using BiomedicalSignalPlotter.Services;
using ScottPlot.Avalonia;

namespace BiomedicalSignalPlotter;

public partial class MainWindow : Window
{
    private const int BufferCapacity = 2_000;
    private readonly CircularSignalBuffer _signalBuffer = new(BufferCapacity);
    private readonly SimulatedDataService _simulatedDataService = new();
    private readonly ISerialService _serialService = new SerialService();
    private readonly RecordingService _recordingService = new();
    private readonly ArduinoCliService _arduinoCliService = new();
    private readonly FirmwareUploadService _firmwareUploadService = new();
    private readonly DispatcherTimer _plotRefreshTimer;
    private AvaPlot[] _plots = [];
    private SignalConfiguration _signalConfiguration = SignalConfigurationService.CreateDefault();
    private int _deviceSampleRateHz = ArduinoDeviceSettingsLimits.DefaultSampleRateHz;
    private string _plotTitle = "Simulated data";
    private bool _isSavingRecording;
    private bool _isUploadingFirmware;
    private bool _isApplyingDeviceSettings;
    private bool _isCheckingArduinoCli;
    private ArduinoDeviceSettings? _pendingDeviceSettings;

    public MainWindow()
    {
        InitializeComponent();

        _plots = [SignalPlot1, SignalPlot2, SignalPlot3];
        ApplyActiveChannelCount(clearBuffer: false);
        ConfigurePlots();
        RefreshPlots();
        _simulatedDataService.SampleGenerated += SimulatedDataService_SampleGenerated;
        _serialService.SampleReceived += SerialService_SampleReceived;
        _serialService.StatusChanged += SerialService_StatusChanged;
        _serialService.MetadataReceived += SerialService_MetadataReceived;
        RefreshSerialPorts(updateStatus: false);

        _plotRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _plotRefreshTimer.Tick += PlotRefreshTimer_Tick;
        _plotRefreshTimer.Start();
        StartSimulation();
        UpdateWorkflowUi();

        Closed += MainWindow_Closed;
    }

    private void ConfigurePlots()
    {
        ApplyPlotGridLayout();

        for (int plotIndex = 0; plotIndex < _plots.Length; plotIndex++)
        {
            if (plotIndex >= _signalConfiguration.PlotLayout.PlotCount)
            {
                continue;
            }

            ConfigurePlot(_plots[plotIndex], plotIndex);
        }
    }

    private void ConfigurePlot(AvaPlot plot, int plotIndex)
    {
        plot.Plot.Title(GetPlotTitle(plotIndex));
        plot.Plot.XLabel("Time (s)");
        plot.Plot.YLabel(GetYAxisLabel(plotIndex));
        plot.Plot.ShowLegend();
    }

    private void SimulatedDataService_SampleGenerated(object? sender, SignalSample sample)
    {
        _signalBuffer.Add(sample);
        _recordingService.Record(sample, RecordedSampleSource.Simulated, _signalConfiguration.ChannelCount);
    }

    private void SerialService_SampleReceived(object? sender, SignalSample sample)
    {
        _signalBuffer.Add(sample);
        _recordingService.Record(sample, RecordedSampleSource.Serial, _signalConfiguration.ChannelCount);
    }

    private void SerialService_StatusChanged(object? sender, string status)
    {
        Dispatcher.UIThread.Post(() => StatusText.Text = status);
    }

    private void SerialService_MetadataReceived(object? sender, string metadata)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DeviceResponseText.Text = $"Device response: {metadata}";

            if (metadata.StartsWith("#ERR ", StringComparison.Ordinal))
            {
                _pendingDeviceSettings = null;
                _isApplyingDeviceSettings = false;
                StatusText.Text = $"Arduino reported an error: {metadata}";
                UpdateWorkflowUi();
                return;
            }

            if (_pendingDeviceSettings is not null &&
                ArduinoStatusReport.TryParse(metadata, out ArduinoStatusReport? status))
            {
                if (status!.Matches(_pendingDeviceSettings))
                {
                    StatusText.Text = $"Device settings verified: {status.ChannelCount} channel(s), {status.AdcBits}-bit ADC, {status.SampleRateHz} Hz.";
                }
                else
                {
                    StatusText.Text = $"Device settings warning: firmware reported {status.ChannelCount} channel(s), {status.AdcBits}-bit ADC, {status.SampleRateHz} Hz.";
                }

                _pendingDeviceSettings = null;
                _isApplyingDeviceSettings = false;
                UpdateWorkflowUi();
            }
        });
    }

    private void PlotRefreshTimer_Tick(object? sender, EventArgs e)
    {
        SignalSnapshot rawSnapshot = _signalBuffer.Snapshot(_signalConfiguration.ChannelCount);
        SignalSnapshot displayedSnapshot = CreateDisplayedSnapshot(rawSnapshot);

        for (int plotIndex = 0; plotIndex < _signalConfiguration.PlotLayout.PlotCount; plotIndex++)
        {
            AvaPlot plot = _plots[plotIndex];
            plot.Plot.Clear();
            ConfigurePlot(plot, plotIndex);

            int[] visibleChannelIndices = GetVisibleChannelIndices(plotIndex).ToArray();
            if (displayedSnapshot.Count > 1 && visibleChannelIndices.Length > 0)
            {
                foreach (int channelIndex in visibleChannelIndices)
                {
                    var channelPlot = plot.Plot.Add.Scatter(
                        displayedSnapshot.TimeSeconds,
                        displayedSnapshot.Channels[channelIndex]);

                    channelPlot.LegendText = FormatLegendText(
                        _signalConfiguration.Channels[channelIndex].Label,
                        channelIndex);
                }

                plot.Plot.Axes.AutoScale();
                ApplyPlotWindow(plot, displayedSnapshot);
            }

            plot.Refresh();
        }

        UpdateWorkflowUi();
    }

    private SignalSnapshot CreateDisplayedSnapshot(SignalSnapshot rawSnapshot)
    {
        if (_signalConfiguration.DisplayMode == SignalDisplayMode.RawAdcCounts)
        {
            return rawSnapshot;
        }

        double[][] channels = Enumerable.Range(0, rawSnapshot.ChannelCount)
            .Select(_ => new double[rawSnapshot.Count])
            .ToArray();

        for (int channelIndex = 0; channelIndex < rawSnapshot.ChannelCount; channelIndex++)
        {
            for (int sampleIndex = 0; sampleIndex < rawSnapshot.Count; sampleIndex++)
            {
                channels[channelIndex][sampleIndex] = SignalConfigurationService.ConvertRawToDisplayValue(
                    rawSnapshot.Channels[channelIndex][sampleIndex],
                    _signalConfiguration);
            }
        }

        return new SignalSnapshot(rawSnapshot.TimeSeconds, channels);
    }

    private void ApplyPlotWindow(AvaPlot plot, SignalSnapshot snapshot)
    {
        double lastTime = snapshot.TimeSeconds[^1];
        double firstVisibleTime = Math.Max(snapshot.TimeSeconds[0], lastTime - _signalConfiguration.PlotWindowSeconds);
        plot.Plot.Axes.SetLimitsX(firstVisibleTime, lastTime);
    }

    private string GetYAxisLabel(int plotIndex)
    {
        if (_signalConfiguration.DisplayMode == SignalDisplayMode.Voltage)
        {
            return "Voltage (V)";
        }

        int[] visibleChannelIndices = GetVisibleChannelIndices(plotIndex).ToArray();
        if (visibleChannelIndices.Length == 0)
        {
            return "Value";
        }

        string firstUnit = _signalConfiguration.Channels[visibleChannelIndices[0]].Unit;
        bool allUnitsMatch = visibleChannelIndices
            .All(channelIndex => _signalConfiguration.Channels[channelIndex].Unit == firstUnit);

        return allUnitsMatch ? firstUnit : "Value";
    }

    private string FormatLegendText(string label, int channelIndex)
    {
        string unit = SignalConfigurationService.GetDisplayUnit(_signalConfiguration, channelIndex);
        return string.IsNullOrWhiteSpace(unit) ? label : $"{label} ({unit})";
    }

    private string GetPlotTitle(int plotIndex)
    {
        string modeDisplayName = SignalConfigurationService.GetModeDisplayName(_signalConfiguration.Mode);

        return _signalConfiguration.PlotLayout.PlotCount == 1
            ? $"{_plotTitle} - {modeDisplayName}"
            : $"{_plotTitle} - Plot {plotIndex + 1} - {modeDisplayName}";
    }

    private IEnumerable<int> GetVisibleChannelIndices(int plotIndex)
    {
        for (int channelIndex = 0; channelIndex < _signalConfiguration.ChannelCount; channelIndex++)
        {
            int? assignedPlotIndex = PlotLayoutConfigurationService.GetPlotIndex(
                _signalConfiguration.PlotLayout.ChannelAssignments[channelIndex]);

            if (assignedPlotIndex == plotIndex)
            {
                yield return channelIndex;
            }
        }
    }

    private void ApplyPlotGridLayout()
    {
        int plotCount = _signalConfiguration.PlotLayout.PlotCount;

        PlotGrid.RowDefinitions.Clear();
        for (int plotIndex = 0; plotIndex < _plots.Length; plotIndex++)
        {
            PlotGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = plotIndex < plotCount
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(0)
            });

            _plots[plotIndex].IsVisible = plotIndex < plotCount;
        }

        PlotGrid.RowSpacing = plotCount > 1 ? 8 : 0;
    }

    private void RefreshPlots()
    {
        foreach (AvaPlot plot in _plots)
        {
            plot.Refresh();
        }
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
        ApplyActiveChannelCount(clearBuffer: true);
        _plotTitle = "Simulated data";
        _simulatedDataService.Start();
        SimulationButton.Content = "Stop Simulation";
        StatusText.Text = $"Simulation running with {_signalConfiguration.ChannelCount} channel(s) at 200 Hz. Plot refreshes at approximately 30 Hz.";
        UpdateWorkflowUi();
    }

    private async Task StopSimulationAsync()
    {
        await _simulatedDataService.StopAsync();
        SimulationButton.Content = "Start Simulation";
        StatusText.Text = $"Simulation stopped. Buffer holds {_signalBuffer.Count} samples.";
        UpdateWorkflowUi();
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

            ApplyActiveChannelCount(clearBuffer: true);
            _plotTitle = "Serial data";
            _serialService.Connect(portName);
            SerialPortComboBox.SelectedItem = portName;
            ConnectButton.Content = "Disconnect";
            SimulationButton.Content = "Start Simulation";
            UpdateWorkflowUi();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ConnectButton.Content = "Connect";
            StatusText.Text = $"Unable to connect to {portName}: {ex.Message}";
            UpdateWorkflowUi();
        }
    }

    private async Task DisconnectSerialAsync()
    {
        await _serialService.DisconnectAsync();
        ConnectButton.Content = "Connect";
        UpdateWorkflowUi();
    }

    private void StartRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isSavingRecording)
        {
            RecordingStatusText.Text = "Wait for save to finish before recording";
            return;
        }

        _recordingService.Start();
        RecordingStatusText.Text = "Recording";
        UpdateWorkflowUi();
    }

    private void StopRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        _recordingService.Stop();
        RecordingStatusText.Text = "Recording stopped";
        UpdateWorkflowUi();
    }

    private async void SaveRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_recordingService.IsRecording)
        {
            RecordingStatusText.Text = "Stop recording before saving";
            return;
        }

        IReadOnlyList<RecordedSample> samples = _recordingService.Snapshot();
        if (samples.Count == 0)
        {
            RecordingStatusText.Text = "No recorded samples to save";
            UpdateWorkflowUi();
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider.CanSave != true)
        {
            RecordingStatusText.Text = "CSV save dialog is not available";
            UpdateWorkflowUi();
            return;
        }

        _isSavingRecording = true;
        UpdateWorkflowUi();

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
            _isSavingRecording = false;
            UpdateWorkflowUi();
            return;
        }

        try
        {
            RecordingStatusText.Text = "Saving CSV...";
            await using Stream stream = await file.OpenWriteAsync();
            await CsvRecordingExporter.WriteAsync(stream, samples, CreateRecordingMetadata());
            RecordingStatusText.Text = $"Saved {samples.Count} samples";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            RecordingStatusText.Text = $"Save failed: {ex.Message}";
        }
        finally
        {
            _isSavingRecording = false;
        }

        UpdateWorkflowUi();
    }

    private void ClearRecordingButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_recordingService.IsRecording)
        {
            RecordingStatusText.Text = "Stop recording before clearing";
            return;
        }

        _recordingService.Clear();
        RecordingStatusText.Text = _recordingService.IsRecording ? "Recording cleared and running" : "Recording cleared";
        UpdateWorkflowUi();
    }

    private async void SignalSettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_recordingService.IsRecording)
        {
            StatusText.Text = "Stop recording before changing signal settings.";
            return;
        }

        if (_recordingService.Count > 0)
        {
            StatusText.Text = "Clear recorded data before changing signal settings.";
            return;
        }

        SignalSettingsWindow dialog = new(_signalConfiguration, _deviceSampleRateHz, isReadOnly: false);
        SignalSettingsResult? result = await dialog.ShowDialog<SignalSettingsResult?>(this);

        if (result is null)
        {
            return;
        }

        int previousChannelCount = _signalConfiguration.ChannelCount;
        _signalConfiguration = result.Configuration;
        _deviceSampleRateHz = result.SampleRateHz;
        ApplyActiveChannelCount(clearBuffer: previousChannelCount != _signalConfiguration.ChannelCount);
        ConfigurePlots();
        RefreshPlots();
        StatusText.Text = $"Signal settings updated: {_signalConfiguration.ChannelCount} channel(s), {_signalConfiguration.AdcBits}-bit ADC, {_deviceSampleRateHz} Hz device sample rate.";
        UpdateWorkflowUi();
    }

    private async void ApplyDeviceSettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_recordingService.IsRecording)
        {
            StatusText.Text = "Stop recording before applying device settings.";
            return;
        }

        if (!_serialService.IsConnected)
        {
            StatusText.Text = "Connect to Arduino before applying device settings.";
            return;
        }

        ArduinoDeviceSettings settings;
        try
        {
            settings = new ArduinoDeviceSettings(
                _signalConfiguration.ChannelCount,
                _signalConfiguration.AdcBits,
                _deviceSampleRateHz);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            StatusText.Text = ex.Message;
            return;
        }

        ApplyActiveChannelCount(clearBuffer: true);

        try
        {
            _isApplyingDeviceSettings = true;
            _pendingDeviceSettings = settings;
            DeviceResponseText.Text = "Device response: waiting for #STATUS";
            UpdateWorkflowUi();

            foreach (string command in ArduinoCommandBuilder.BuildApplySettingsSequence(settings))
            {
                await _serialService.SendLineAsync(command);
            }

            StatusText.Text = "Device settings sent; waiting for firmware #STATUS verification.";
            _isApplyingDeviceSettings = false;
            UpdateWorkflowUi();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _pendingDeviceSettings = null;
            _isApplyingDeviceSettings = false;
            StatusText.Text = $"Unable to apply device settings: {ex.Message}";
            UpdateWorkflowUi();
        }
    }

    private async void CheckArduinoCliButton_Click(object? sender, RoutedEventArgs e)
    {
        _isCheckingArduinoCli = true;
        UpdateWorkflowUi();
        StatusText.Text = "Checking Arduino CLI...";

        try
        {
            ArduinoCliCheckResult result = await _arduinoCliService.CheckAvailabilityAsync();
            StatusText.Text = result.Message;
        }
        finally
        {
            _isCheckingArduinoCli = false;
            UpdateWorkflowUi();
        }
    }

    private async void UploadFirmwareButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_recordingService.IsRecording)
        {
            StatusText.Text = "Stop recording before uploading firmware.";
            return;
        }

        _isUploadingFirmware = true;
        UpdateWorkflowUi();

        try
        {
            bool reconnectAfterUpload = _serialService.IsConnected;
            string? previousPortName = _serialService.PortName;

            if (_serialService.IsConnected)
            {
                StatusText.Text = "Disconnecting serial before firmware upload...";
                await DisconnectSerialAsync();
            }

            Progress<string> progress = new(message => StatusText.Text = message);
            FirmwareUploadResult result = await _firmwareUploadService.UploadUnoR4WifiAsync(progress);
            StatusText.Text = result.Message;

            if (result.Succeeded)
            {
                RefreshSerialPorts(updateStatus: false);
                if (reconnectAfterUpload)
                {
                    string reconnectPort = result.PortName ?? previousPortName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(reconnectPort))
                    {
                        await TryReconnectAfterFirmwareUploadAsync(reconnectPort);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        {
            StatusText.Text = $"Upload failed: {ex.Message}";
        }
        finally
        {
            _isUploadingFirmware = false;
            UpdateWorkflowUi();
        }
    }

    private async Task TryReconnectAfterFirmwareUploadAsync(string portName)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(750));

        try
        {
            ApplyActiveChannelCount(clearBuffer: true);
            _plotTitle = "Serial data";
            _serialService.Connect(portName);
            ConnectButton.Content = "Disconnect";
            SimulationButton.Content = "Start Simulation";
            StatusText.Text = $"Upload succeeded and reconnected to {portName}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ConnectButton.Content = "Connect";
            StatusText.Text = $"Upload succeeded, but reconnect to {portName} failed: {ex.Message}";
        }
    }

    private void UpdateWorkflowUi()
    {
        int count = _recordingService.Count;
        bool isRecording = _recordingService.IsRecording;
        bool hasRecordedSamples = count > 0;
        bool isBusy = _isSavingRecording || _isUploadingFirmware || _isApplyingDeviceSettings;
        bool canEditMetadata = !hasRecordedSamples && !isRecording && !isBusy;
        bool canEditDeviceSettings = !isRecording && !isBusy;

        RecordedSampleCountText.Text = count == 1 ? "1 sample" : $"{count} samples";
        StartRecordingButton.IsEnabled = !isRecording && !_isSavingRecording && !_isUploadingFirmware;
        StopRecordingButton.IsEnabled = isRecording;
        SaveRecordingButton.IsEnabled = hasRecordedSamples && !isRecording && !_isSavingRecording;
        ClearRecordingButton.IsEnabled = hasRecordedSamples && !isRecording && !_isSavingRecording;

        SerialPortComboBox.IsEnabled = !_serialService.IsConnected && !_isUploadingFirmware;
        RefreshPortsButton.IsEnabled = !_serialService.IsConnected && !_isUploadingFirmware;
        ConnectButton.IsEnabled = !_isUploadingFirmware;
        SimulationButton.IsEnabled = !_isUploadingFirmware;

        SignalSettingsButton.IsEnabled = canEditMetadata;
        ApplyDeviceSettingsButton.IsEnabled = _serialService.IsConnected && canEditDeviceSettings;

        CheckArduinoCliButton.IsEnabled = !_isCheckingArduinoCli && !_isUploadingFirmware;
        UploadFirmwareButton.IsEnabled = !isRecording && !_isSavingRecording && !_isUploadingFirmware;
    }

    private void ApplyActiveChannelCount(bool clearBuffer)
    {
        _simulatedDataService.SetChannelCount(_signalConfiguration.ChannelCount);
        _serialService.ExpectedChannelCount = _signalConfiguration.ChannelCount;

        if (clearBuffer)
        {
            _signalBuffer.Clear();
        }
    }

    private CsvRecordingMetadata CreateRecordingMetadata()
    {
        string[] labels = _signalConfiguration.Channels
            .Take(_signalConfiguration.ChannelCount)
            .Select(channel => channel.Label)
            .ToArray();
        string[] units = Enumerable.Range(0, _signalConfiguration.ChannelCount)
            .Select(channelIndex => SignalConfigurationService.GetDisplayUnit(_signalConfiguration, channelIndex))
            .ToArray();

        return new CsvRecordingMetadata(
            SignalConfigurationService.GetModeDisplayName(_signalConfiguration.Mode),
            _signalConfiguration.ChannelCount,
            labels,
            units,
            _signalConfiguration.AdcBits,
            _signalConfiguration.ReferenceVoltage,
            SignalConfigurationService.GetDisplayModeText(_signalConfiguration.DisplayMode));
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _plotRefreshTimer.Stop();
        await _serialService.DisposeAsync();
        await _simulatedDataService.StopAsync();
    }

}
