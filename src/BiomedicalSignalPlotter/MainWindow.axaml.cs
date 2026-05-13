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
    private readonly int[] _channelCountOptions = Enumerable
        .Range(AnalogChannelLimits.Minimum, AnalogChannelLimits.Maximum)
        .ToArray();
    private readonly DisplayModeOption[] _displayModeOptions =
    [
        new(SignalDisplayMode.RawAdcCounts, "Raw ADC counts"),
        new(SignalDisplayMode.Voltage, "Voltage")
    ];
    private TextBox[] _channelLabelTextBoxes = [];
    private TextBox[] _channelUnitTextBoxes = [];
    private Control[] _channelConfigurationPanels = [];
    private SignalConfiguration _signalConfiguration = SignalConfigurationService.CreateDefault();
    private string _plotTitle = "Simulated data";
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

    private void PlotRefreshTimer_Tick(object? sender, EventArgs e)
    {
        SignalSnapshot rawSnapshot = _signalBuffer.Snapshot(_signalConfiguration.ChannelCount);
        SignalSnapshot displayedSnapshot = CreateDisplayedSnapshot(rawSnapshot);

        SignalPlot.Plot.Clear();
        ConfigurePlot();

        if (displayedSnapshot.Count > 1)
        {
            for (int channelIndex = 0; channelIndex < displayedSnapshot.ChannelCount; channelIndex++)
            {
                var channelPlot = SignalPlot.Plot.Add.Scatter(
                    displayedSnapshot.TimeSeconds,
                    displayedSnapshot.Channels[channelIndex]);

                channelPlot.LegendText = FormatLegendText(
                    _signalConfiguration.Channels[channelIndex].Label,
                    channelIndex);
            }

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

        string firstUnit = _signalConfiguration.Channels[0].Unit;
        bool allUnitsMatch = _signalConfiguration.Channels
            .Take(_signalConfiguration.ChannelCount)
            .All(channel => channel.Unit == firstUnit);

        return allUnitsMatch ? firstUnit : "Value";
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
        ApplyActiveChannelCount(clearBuffer: true);
        _plotTitle = "Simulated data";
        _simulatedDataService.Start();
        SimulationButton.Content = "Stop Simulation";
        StatusText.Text = $"Simulation running with {_signalConfiguration.ChannelCount} channel(s) at 200 Hz. Plot refreshes at approximately 30 Hz.";
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

            ApplyActiveChannelCount(clearBuffer: true);
            _plotTitle = "Serial data";
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
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
        SignalModeComboBox.IsEnabled = count == 0 && !_recordingService.IsRecording;
        ChannelCountComboBox.IsEnabled = count == 0 && !_recordingService.IsRecording;
    }

    private void InitializeConfigurationUi()
    {
        _channelLabelTextBoxes =
        [
            Channel0LabelTextBox,
            Channel1LabelTextBox,
            Channel2LabelTextBox,
            Channel3LabelTextBox,
            Channel4LabelTextBox,
            Channel5LabelTextBox
        ];
        _channelUnitTextBoxes =
        [
            Channel0UnitTextBox,
            Channel1UnitTextBox,
            Channel2UnitTextBox,
            Channel3UnitTextBox,
            Channel4UnitTextBox,
            Channel5UnitTextBox
        ];
        _channelConfigurationPanels =
        [
            Channel0Panel,
            Channel1Panel,
            Channel2Panel,
            Channel3Panel,
            Channel4Panel,
            Channel5Panel
        ];

        SignalModeComboBox.ItemsSource = SignalConfigurationService.AllPresets;
        ChannelCountComboBox.ItemsSource = _channelCountOptions;
        DisplayModeComboBox.ItemsSource = _displayModeOptions;
        ApplyActiveChannelCount(clearBuffer: false);
        UpdateConfigurationUi();
    }

    private void UpdateConfigurationUi()
    {
        _isUpdatingConfigurationUi = true;
        try
        {
            SignalModeComboBox.SelectedItem = SignalConfigurationService.GetPreset(_signalConfiguration.Mode);
            ChannelCountComboBox.SelectedItem = _signalConfiguration.ChannelCount;
            DisplayModeComboBox.SelectedItem = _displayModeOptions.Single(option => option.DisplayMode == _signalConfiguration.DisplayMode);

            for (int i = 0; i < AnalogChannelLimits.Maximum; i++)
            {
                _channelLabelTextBoxes[i].Text = _signalConfiguration.Channels[i].Label;
                _channelUnitTextBoxes[i].Text = _signalConfiguration.Channels[i].Unit;
                _channelConfigurationPanels[i].IsVisible = i < _signalConfiguration.ChannelCount;
            }

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

        _signalConfiguration = preset.Mode == SignalMode.Custom
            ? SignalConfigurationService.SwitchToCustomPreservingSettings(_signalConfiguration)
            : SignalConfigurationService.ApplyPreset(preset.Mode);

        ApplyActiveChannelCount(clearBuffer: true);
        UpdateConfigurationUi();
    }

    private void ChannelCountComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingConfigurationUi || ChannelCountComboBox.SelectedItem is not int channelCount)
        {
            return;
        }

        _signalConfiguration = SignalConfigurationService.ApplyChannelCount(_signalConfiguration, channelCount);
        ApplyActiveChannelCount(clearBuffer: true);
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
            _channelLabelTextBoxes.Select(textBox => textBox.Text ?? string.Empty).ToArray(),
            _channelUnitTextBoxes.Select(textBox => textBox.Text ?? string.Empty).ToArray());

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
