using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BiomedicalSignalPlotter.Arduino;
using BiomedicalSignalPlotter.Configuration;
using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter;

public partial class SignalSettingsWindow : Window
{
    private readonly int[] _channelCountOptions = Enumerable
        .Range(AnalogChannelLimits.Minimum, AnalogChannelLimits.Maximum)
        .ToArray();
    private readonly int[] _plotCountOptions = Enumerable
        .Range(PlotLayoutConfigurationService.MinimumPlotCount, PlotLayoutConfigurationService.MaximumPlotCount)
        .ToArray();
    private readonly DisplayModeOption[] _displayModeOptions =
    [
        new(SignalDisplayMode.RawAdcCounts, "Raw ADC counts"),
        new(SignalDisplayMode.Voltage, "Voltage")
    ];
    private readonly bool _isReadOnly;
    private TextBox[] _channelLabelTextBoxes = [];
    private TextBox[] _channelUnitTextBoxes = [];
    private ComboBox[] _channelPlotComboBoxes = [];
    private Control[] _channelConfigurationPanels = [];
    private SignalConfiguration _configuration;
    private int _sampleRateHz;
    private bool _isUpdatingUi;

    public SignalSettingsWindow()
        : this(SignalConfigurationService.CreateDefault(), ArduinoDeviceSettingsLimits.DefaultSampleRateHz, isReadOnly: true)
    {
    }

    public SignalSettingsWindow(SignalConfiguration configuration, int sampleRateHz, bool isReadOnly)
    {
        _configuration = configuration with
        {
            Channels = configuration.Channels.ToArray(),
            PlotLayout = new PlotLayoutConfiguration(
                configuration.PlotLayout.PlotCount,
                configuration.PlotLayout.ChannelAssignments,
                configuration.PlotLayout.PlotPanels)
        };
        _sampleRateHz = sampleRateHz;
        _isReadOnly = isReadOnly;

        InitializeComponent();
        InitializeSettingsUi();
    }

    public SignalSettingsResult? Result { get; private set; }

    private void InitializeSettingsUi()
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
        _channelPlotComboBoxes =
        [
            Channel0PlotComboBox,
            Channel1PlotComboBox,
            Channel2PlotComboBox,
            Channel3PlotComboBox,
            Channel4PlotComboBox,
            Channel5PlotComboBox
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
        PlotCountComboBox.ItemsSource = _plotCountOptions;
        DisplayModeComboBox.ItemsSource = _displayModeOptions;
        UpdateSettingsUi();
        ApplyReadOnlyState();
    }

    private void UpdateSettingsUi()
    {
        _isUpdatingUi = true;
        try
        {
            SignalModeComboBox.SelectedItem = SignalConfigurationService.GetPreset(_configuration.Mode);
            ChannelCountComboBox.SelectedItem = _configuration.ChannelCount;
            PlotCountComboBox.SelectedItem = _configuration.PlotLayout.PlotCount;
            DisplayModeComboBox.SelectedItem = _displayModeOptions.Single(option => option.DisplayMode == _configuration.DisplayMode);
            PlotAssignmentOption[] plotAssignmentOptions = CreatePlotAssignmentOptions(_configuration.PlotLayout.PlotCount);

            for (int i = 0; i < AnalogChannelLimits.Maximum; i++)
            {
                _channelLabelTextBoxes[i].Text = _configuration.Channels[i].Label;
                _channelUnitTextBoxes[i].Text = _configuration.Channels[i].Unit;
                _channelConfigurationPanels[i].IsVisible = i < _configuration.ChannelCount;
                _channelPlotComboBoxes[i].ItemsSource = plotAssignmentOptions;
                _channelPlotComboBoxes[i].SelectedItem = plotAssignmentOptions.FirstOrDefault(
                    option => option.Assignment == _configuration.PlotLayout.ChannelAssignments[i])
                    ?? plotAssignmentOptions.Single(option => option.Assignment == ChannelPlotAssignment.Plot1);
            }

            AdcBitsTextBox.Text = _configuration.AdcBits.ToString(CultureInfo.InvariantCulture);
            SampleRateHzTextBox.Text = _sampleRateHz.ToString(CultureInfo.InvariantCulture);
            ReferenceVoltageTextBox.Text = SignalConfigurationService.FormatReferenceVoltage(_configuration.ReferenceVoltage);
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void ApplyReadOnlyState()
    {
        if (!_isReadOnly)
        {
            return;
        }

        SignalModeComboBox.IsEnabled = false;
        ChannelCountComboBox.IsEnabled = false;
        PlotCountComboBox.IsEnabled = false;
        DisplayModeComboBox.IsEnabled = false;
        AdcBitsTextBox.IsReadOnly = true;
        SampleRateHzTextBox.IsReadOnly = true;
        ReferenceVoltageTextBox.IsReadOnly = true;

        foreach (TextBox textBox in _channelLabelTextBoxes.Concat(_channelUnitTextBoxes))
        {
            textBox.IsReadOnly = true;
        }

        foreach (ComboBox comboBox in _channelPlotComboBoxes)
        {
            comboBox.IsEnabled = false;
        }

        SettingsStatusText.Text = "Settings are read-only while recording or while recorded samples are present.";
    }

    private void SignalModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || _isReadOnly || SignalModeComboBox.SelectedItem is not SignalModePreset preset)
        {
            return;
        }

        PlotLayoutConfiguration currentPlotLayout = _configuration.PlotLayout;
        _configuration = preset.Mode == SignalMode.Custom
            ? SignalConfigurationService.SwitchToCustomPreservingSettings(_configuration)
            : SignalConfigurationService.ApplyPreset(preset.Mode) with { PlotLayout = currentPlotLayout };

        UpdateSettingsUi();
    }

    private void ChannelCountComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || _isReadOnly || ChannelCountComboBox.SelectedItem is not int channelCount)
        {
            return;
        }

        _configuration = SignalConfigurationService.ApplyChannelCount(_configuration, channelCount);
        UpdateSettingsUi();
    }

    private void PlotCountComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || _isReadOnly || PlotCountComboBox.SelectedItem is not int plotCount)
        {
            return;
        }

        _configuration = SignalConfigurationService.ApplyPlotCount(_configuration, plotCount);
        UpdateSettingsUi();
    }

    private void DisplayModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || _isReadOnly || DisplayModeComboBox.SelectedItem is not DisplayModeOption option)
        {
            return;
        }

        _configuration = SignalConfigurationService.ApplyDisplayMode(_configuration, option.DisplayMode);
    }

    private void ChannelPlotComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || _isReadOnly)
        {
            return;
        }

        _configuration = SignalConfigurationService.ApplyChannelPlotAssignments(
            _configuration,
            _channelPlotComboBoxes
                .Select(comboBox => comboBox.SelectedItem is PlotAssignmentOption option
                    ? option.Assignment
                    : ChannelPlotAssignment.Plot1)
                .ToArray());
    }

    private void ChannelConfigurationTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingUi || _isReadOnly)
        {
            return;
        }

        _configuration = SignalConfigurationService.ApplyManualChannelTextEdit(
            _configuration,
            _channelLabelTextBoxes.Select(textBox => textBox.Text ?? string.Empty).ToArray(),
            _channelUnitTextBoxes.Select(textBox => textBox.Text ?? string.Empty).ToArray());

        _isUpdatingUi = true;
        try
        {
            SignalModeComboBox.SelectedItem = SignalConfigurationService.GetPreset(SignalMode.Custom);
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        int adcBits = TryParseInt(AdcBitsTextBox.Text, _configuration.AdcBits);
        int sampleRateHz = TryParseInt(SampleRateHzTextBox.Text, _sampleRateHz);
        double referenceVoltage = TryParseDouble(ReferenceVoltageTextBox.Text, _configuration.ReferenceVoltage);

        try
        {
            ArduinoDeviceSettingsLimits.ValidateAdcBits(adcBits);
            ArduinoDeviceSettingsLimits.ValidateSampleRateHz(sampleRateHz);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            SettingsStatusText.Text = ex.Message;
            return;
        }

        _configuration = SignalConfigurationService.ApplyAdcSettings(_configuration, adcBits, referenceVoltage);
        _sampleRateHz = sampleRateHz;
        Result = new SignalSettingsResult(_configuration, _sampleRateHz);
        Close(Result);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
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

    private sealed record DisplayModeOption(SignalDisplayMode DisplayMode, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private static PlotAssignmentOption[] CreatePlotAssignmentOptions(int plotCount)
    {
        return PlotLayoutConfigurationService.GetAvailableAssignments(plotCount)
            .Select(assignment => new PlotAssignmentOption(
                assignment,
                PlotLayoutConfigurationService.GetAssignmentDisplayName(assignment)))
            .ToArray();
    }

    private sealed record PlotAssignmentOption(ChannelPlotAssignment Assignment, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
