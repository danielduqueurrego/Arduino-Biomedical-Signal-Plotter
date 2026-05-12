using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BiomedicalSignalPlotter.Configuration;
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
    private readonly DisplayModeOption[] _displayModeOptions =
    [
        new(SignalDisplayMode.RawAdcCounts, "Raw ADC counts"),
        new(SignalDisplayMode.Voltage, "Voltage")
    ];
    private SignalConfiguration _signalConfiguration = SignalConfigurationService.CreateDefault();
    private string _plotTitle = "Simulated two-channel data";
    private bool _isUpdatingConfigurationUi;

    public MainWindow()
    {
        InitializeComponent();

        InitializeConfigurationUi();
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
        SignalPlot.Plot.Title($"{_plotTitle} - {SignalConfigurationService.GetModeDisplayName(_signalConfiguration.Mode)}");
        SignalPlot.Plot.XLabel("Time (s)");
        SignalPlot.Plot.YLabel(GetYAxisLabel());
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
        SignalSnapshot rawSnapshot = _signalBuffer.Snapshot();
        SignalSnapshot displayedSnapshot = CreateDisplayedSnapshot(rawSnapshot);

        SignalPlot.Plot.Clear();
        ConfigurePlot();

        if (displayedSnapshot.Count > 1)
        {
            var channel0 = SignalPlot.Plot.Add.Scatter(displayedSnapshot.TimeSeconds, displayedSnapshot.Channel1);
            channel0.LegendText = FormatLegendText(_signalConfiguration.Channel0.Label, 0);

            var channel1 = SignalPlot.Plot.Add.Scatter(displayedSnapshot.TimeSeconds, displayedSnapshot.Channel2);
            channel1.LegendText = FormatLegendText(_signalConfiguration.Channel1.Label, 1);

            SignalPlot.Plot.Axes.AutoScale();
            ApplyPlotWindow(displayedSnapshot);
        }

        SignalPlot.Refresh();
        UpdateRecordingUi();
    }

    private SignalSnapshot CreateDisplayedSnapshot(SignalSnapshot rawSnapshot)
    {
        if (_signalConfiguration.DisplayMode == SignalDisplayMode.RawAdcCounts)
        {
            return rawSnapshot;
        }

        double[] channel0 = new double[rawSnapshot.Count];
        double[] channel1 = new double[rawSnapshot.Count];

        for (int i = 0; i < rawSnapshot.Count; i++)
        {
            channel0[i] = SignalConfigurationService.ConvertRawToDisplayValue(rawSnapshot.Channel1[i], _signalConfiguration);
            channel1[i] = SignalConfigurationService.ConvertRawToDisplayValue(rawSnapshot.Channel2[i], _signalConfiguration);
        }

        return new SignalSnapshot(rawSnapshot.TimeSeconds, channel0, channel1);
    }

    private void ApplyPlotWindow(SignalSnapshot snapshot)
    {
        double lastTime = snapshot.TimeSeconds[^1];
        double firstVisibleTime = Math.Max(snapshot.TimeSeconds[0], lastTime - _signalConfiguration.PlotWindowSeconds);
        SignalPlot.Plot.Axes.SetLimitsX(firstVisibleTime, lastTime);
    }

    private string GetYAxisLabel()
    {
        if (_signalConfiguration.DisplayMode == SignalDisplayMode.Voltage)
        {
            return "Voltage (V)";
        }

        return _signalConfiguration.Channel0.Unit == _signalConfiguration.Channel1.Unit
            ? _signalConfiguration.Channel0.Unit
            : "Value";
    }

    private string FormatLegendText(string label, int channelIndex)
    {
        string unit = SignalConfigurationService.GetDisplayUnit(_signalConfiguration, channelIndex);
        return string.IsNullOrWhiteSpace(unit) ? label : $"{label} ({unit})";
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
            await CsvRecordingExporter.WriteAsync(stream, samples, CreateRecordingMetadata());
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

    private void InitializeConfigurationUi()
    {
        SignalModeComboBox.ItemsSource = SignalConfigurationService.AllPresets;
        DisplayModeComboBox.ItemsSource = _displayModeOptions;
        UpdateConfigurationUi();
    }

    private void UpdateConfigurationUi()
    {
        _isUpdatingConfigurationUi = true;
        try
        {
            SignalModeComboBox.SelectedItem = SignalConfigurationService.GetPreset(_signalConfiguration.Mode);
            DisplayModeComboBox.SelectedItem = _displayModeOptions.Single(option => option.DisplayMode == _signalConfiguration.DisplayMode);
            Channel0LabelTextBox.Text = _signalConfiguration.Channel0.Label;
            Channel0UnitTextBox.Text = _signalConfiguration.Channel0.Unit;
            Channel1LabelTextBox.Text = _signalConfiguration.Channel1.Label;
            Channel1UnitTextBox.Text = _signalConfiguration.Channel1.Unit;
            AdcBitsTextBox.Text = _signalConfiguration.AdcBits.ToString(CultureInfo.InvariantCulture);
            ReferenceVoltageTextBox.Text = SignalConfigurationService.FormatReferenceVoltage(_signalConfiguration.ReferenceVoltage);
        }
        finally
        {
            _isUpdatingConfigurationUi = false;
        }
    }

    private void SignalModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingConfigurationUi || SignalModeComboBox.SelectedItem is not SignalModePreset preset)
        {
            return;
        }

        _signalConfiguration = SignalConfigurationService.ApplyPreset(preset.Mode);
        UpdateConfigurationUi();
    }

    private void DisplayModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingConfigurationUi || DisplayModeComboBox.SelectedItem is not DisplayModeOption option)
        {
            return;
        }

        _signalConfiguration = SignalConfigurationService.ApplyDisplayMode(_signalConfiguration, option.DisplayMode);
    }

    private void ChannelConfigurationTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingConfigurationUi)
        {
            return;
        }

        _signalConfiguration = SignalConfigurationService.ApplyManualChannelTextEdit(
            _signalConfiguration,
            Channel0LabelTextBox.Text ?? string.Empty,
            Channel0UnitTextBox.Text ?? string.Empty,
            Channel1LabelTextBox.Text ?? string.Empty,
            Channel1UnitTextBox.Text ?? string.Empty);

        _isUpdatingConfigurationUi = true;
        try
        {
            SignalModeComboBox.SelectedItem = SignalConfigurationService.GetPreset(SignalMode.Custom);
        }
        finally
        {
            _isUpdatingConfigurationUi = false;
        }
    }

    private void AdcConfigurationTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        int adcBits = TryParseInt(AdcBitsTextBox.Text, _signalConfiguration.AdcBits);
        double referenceVoltage = TryParseDouble(ReferenceVoltageTextBox.Text, _signalConfiguration.ReferenceVoltage);

        _signalConfiguration = SignalConfigurationService.ApplyAdcSettings(_signalConfiguration, adcBits, referenceVoltage);
        UpdateConfigurationUi();
    }

    private CsvRecordingMetadata CreateRecordingMetadata()
    {
        return new CsvRecordingMetadata(
            SignalConfigurationService.GetModeDisplayName(_signalConfiguration.Mode),
            _signalConfiguration.Channel0.Label,
            _signalConfiguration.Channel0.Unit,
            _signalConfiguration.Channel1.Label,
            _signalConfiguration.Channel1.Unit,
            _signalConfiguration.AdcBits,
            _signalConfiguration.ReferenceVoltage,
            SignalConfigurationService.GetDisplayModeText(_signalConfiguration.DisplayMode));
    }

    private static int TryParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : fallback;
    }

    private static double TryParseDouble(string? value, double fallback)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double invariantResult))
        {
            return invariantResult;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out double currentCultureResult)
            ? currentCultureResult
            : fallback;
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _plotRefreshTimer.Stop();
        await _serialService.DisposeAsync();
        await _simulatedDataService.StopAsync();
    }

    private sealed record DisplayModeOption(SignalDisplayMode DisplayMode, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
