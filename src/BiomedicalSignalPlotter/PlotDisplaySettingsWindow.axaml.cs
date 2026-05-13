using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using BiomedicalSignalPlotter.Configuration;

namespace BiomedicalSignalPlotter;

public partial class PlotDisplaySettingsWindow : Window
{
    private readonly TimeWindowOption[] _timeWindowOptions =
    [
        new(2.0, "2 s"),
        new(5.0, "5 s"),
        new(10.0, "10 s"),
        new(30.0, "30 s"),
        new(null, "Custom")
    ];
    private CheckBox[] _autoYCheckBoxes = [];
    private TextBox[] _yMinimumTextBoxes = [];
    private TextBox[] _yMaximumTextBoxes = [];
    private CheckBox[][] _referenceBarEnabledCheckBoxes = [];
    private TextBox[][] _referenceBarValueTextBoxes = [];
    private TextBox[][] _referenceBarLabelTextBoxes = [];
    private StackPanel[] _referenceBarPanels = [];
    private Control[] _plotPanels = [];
    private SignalConfiguration _configuration;
    private bool _isUpdatingUi;

    public PlotDisplaySettingsWindow()
        : this(SignalConfigurationService.CreateDefault())
    {
    }

    public PlotDisplaySettingsWindow(SignalConfiguration configuration)
    {
        _configuration = configuration with
        {
            Channels = configuration.Channels.ToArray(),
            PlotLayout = new PlotLayoutConfiguration(
                configuration.PlotLayout.PlotCount,
                configuration.PlotLayout.ChannelAssignments,
                configuration.PlotLayout.PlotPanels)
        };

        InitializeComponent();
        InitializeSettingsUi();
    }

    public SignalConfiguration? Result { get; private set; }

    private void InitializeSettingsUi()
    {
        _autoYCheckBoxes =
        [
            Plot1AutoYCheckBox,
            Plot2AutoYCheckBox,
            Plot3AutoYCheckBox
        ];
        _yMinimumTextBoxes =
        [
            Plot1YMinimumTextBox,
            Plot2YMinimumTextBox,
            Plot3YMinimumTextBox
        ];
        _yMaximumTextBoxes =
        [
            Plot1YMaximumTextBox,
            Plot2YMaximumTextBox,
            Plot3YMaximumTextBox
        ];
        _plotPanels =
        [
            Plot1Panel,
            Plot2Panel,
            Plot3Panel
        ];
        _referenceBarPanels =
        [
            Plot1ReferenceBarsPanel,
            Plot2ReferenceBarsPanel,
            Plot3ReferenceBarsPanel
        ];

        InitializeReferenceBarControls();
        TimeWindowComboBox.ItemsSource = _timeWindowOptions;
        UpdateSettingsUi();
    }

    private void UpdateSettingsUi()
    {
        _isUpdatingUi = true;
        try
        {
            TimeWindowOption selectedOption = _timeWindowOptions.FirstOrDefault(
                option => option.Seconds is not null &&
                    Math.Abs(option.Seconds.Value - _configuration.PlotWindowSeconds) < 0.000001)
                ?? _timeWindowOptions.Single(option => option.Seconds is null);

            TimeWindowComboBox.SelectedItem = selectedOption;
            CustomTimeWindowTextBox.Text = FormatDouble(_configuration.PlotWindowSeconds);
            CustomTimeWindowTextBox.IsEnabled = selectedOption.Seconds is null;

            for (int plotIndex = 0; plotIndex < PlotLayoutConfigurationService.MaximumPlotCount; plotIndex++)
            {
                PlotPanelConfiguration panel = _configuration.PlotLayout.PlotPanels[plotIndex];
                _plotPanels[plotIndex].IsVisible = plotIndex < _configuration.PlotLayout.PlotCount;
                _autoYCheckBoxes[plotIndex].IsChecked = panel.UseAutoYRange;
                _yMinimumTextBoxes[plotIndex].Text = FormatDouble(panel.ManualYMinimum);
                _yMaximumTextBoxes[plotIndex].Text = FormatDouble(panel.ManualYMaximum);

                for (int referenceBarIndex = 0; referenceBarIndex < PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot; referenceBarIndex++)
                {
                    ReferenceBarConfiguration referenceBar = panel.ReferenceBars[referenceBarIndex];
                    _referenceBarEnabledCheckBoxes[plotIndex][referenceBarIndex].IsChecked = referenceBar.IsEnabled;
                    _referenceBarValueTextBoxes[plotIndex][referenceBarIndex].Text = FormatDouble(referenceBar.Value);
                    _referenceBarLabelTextBoxes[plotIndex][referenceBarIndex].Text = referenceBar.Label;
                }
            }
        }
        finally
        {
            _isUpdatingUi = false;
        }

        UpdateYRangeTextBoxStates();
        UpdateReferenceBarTextBoxStates();
    }

    private void TimeWindowComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || TimeWindowComboBox.SelectedItem is not TimeWindowOption option)
        {
            return;
        }

        CustomTimeWindowTextBox.IsEnabled = option.Seconds is null;
        if (option.Seconds is not null)
        {
            CustomTimeWindowTextBox.Text = FormatDouble(option.Seconds.Value);
        }
    }

    private void AutoYCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        if (!_isUpdatingUi)
        {
            UpdateYRangeTextBoxStates();
        }
    }

    private void ReferenceBarCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        if (!_isUpdatingUi)
        {
            UpdateReferenceBarTextBoxStates();
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!TryResolvePlotWindowSeconds(out double plotWindowSeconds))
        {
            return;
        }

        try
        {
            SignalConfiguration updatedConfiguration =
                SignalConfigurationService.ApplyPlotWindowSeconds(_configuration, plotWindowSeconds);
            PlotLayoutConfiguration plotLayout = updatedConfiguration.PlotLayout;

            for (int plotIndex = 0; plotIndex < plotLayout.PlotCount; plotIndex++)
            {
                PlotPanelConfiguration currentPanel = plotLayout.PlotPanels[plotIndex];
                bool useAutoYRange = _autoYCheckBoxes[plotIndex].IsChecked == true;
                double manualYMinimum = currentPanel.ManualYMinimum;
                double manualYMaximum = currentPanel.ManualYMaximum;

                if (!useAutoYRange)
                {
                    if (!TryParseDouble(_yMinimumTextBoxes[plotIndex].Text, out manualYMinimum) ||
                        !TryParseDouble(_yMaximumTextBoxes[plotIndex].Text, out manualYMaximum))
                    {
                        PlotDisplayStatusText.Text = $"Enter numeric Y limits for Plot {plotIndex + 1}.";
                        return;
                    }
                }

                plotLayout = PlotLayoutConfigurationService.ApplyPanelYRange(
                    plotLayout,
                    plotIndex,
                    useAutoYRange,
                    manualYMinimum,
                    manualYMaximum);

                for (int referenceBarIndex = 0; referenceBarIndex < PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot; referenceBarIndex++)
                {
                    ReferenceBarConfiguration currentBar = plotLayout.PlotPanels[plotIndex].ReferenceBars[referenceBarIndex];
                    bool isEnabled = _referenceBarEnabledCheckBoxes[plotIndex][referenceBarIndex].IsChecked == true;
                    double value = currentBar.Value;

                    if (isEnabled &&
                        !TryParseDouble(_referenceBarValueTextBoxes[plotIndex][referenceBarIndex].Text, out value))
                    {
                        PlotDisplayStatusText.Text = $"Enter a numeric reference bar value for Plot {plotIndex + 1}, Bar {referenceBarIndex + 1}.";
                        return;
                    }

                    plotLayout = PlotLayoutConfigurationService.ApplyReferenceBar(
                        plotLayout,
                        plotIndex,
                        referenceBarIndex,
                        isEnabled,
                        value,
                        _referenceBarLabelTextBoxes[plotIndex][referenceBarIndex].Text ?? string.Empty);
                }
            }

            Result = updatedConfiguration with { PlotLayout = plotLayout };
            Close(Result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            PlotDisplayStatusText.Text = ex.Message;
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private bool TryResolvePlotWindowSeconds(out double plotWindowSeconds)
    {
        if (TimeWindowComboBox.SelectedItem is TimeWindowOption { Seconds: double seconds })
        {
            plotWindowSeconds = seconds;
            return true;
        }

        if (!TryParseDouble(CustomTimeWindowTextBox.Text, out plotWindowSeconds))
        {
            PlotDisplayStatusText.Text = "Enter a positive numeric plot time window in seconds.";
            return false;
        }

        try
        {
            SignalConfigurationService.ValidatePlotWindowSeconds(plotWindowSeconds);
            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            PlotDisplayStatusText.Text = ex.Message;
            return false;
        }
    }

    private void UpdateYRangeTextBoxStates()
    {
        for (int plotIndex = 0; plotIndex < PlotLayoutConfigurationService.MaximumPlotCount; plotIndex++)
        {
            bool useAutoYRange = _autoYCheckBoxes[plotIndex].IsChecked == true;
            _yMinimumTextBoxes[plotIndex].IsEnabled = !useAutoYRange;
            _yMaximumTextBoxes[plotIndex].IsEnabled = !useAutoYRange;
        }
    }

    private void UpdateReferenceBarTextBoxStates()
    {
        for (int plotIndex = 0; plotIndex < PlotLayoutConfigurationService.MaximumPlotCount; plotIndex++)
        {
            for (int referenceBarIndex = 0; referenceBarIndex < PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot; referenceBarIndex++)
            {
                bool isEnabled = _referenceBarEnabledCheckBoxes[plotIndex][referenceBarIndex].IsChecked == true;
                _referenceBarValueTextBoxes[plotIndex][referenceBarIndex].IsEnabled = isEnabled;
                _referenceBarLabelTextBoxes[plotIndex][referenceBarIndex].IsEnabled = isEnabled;
            }
        }
    }

    private void InitializeReferenceBarControls()
    {
        _referenceBarEnabledCheckBoxes = new CheckBox[PlotLayoutConfigurationService.MaximumPlotCount][];
        _referenceBarValueTextBoxes = new TextBox[PlotLayoutConfigurationService.MaximumPlotCount][];
        _referenceBarLabelTextBoxes = new TextBox[PlotLayoutConfigurationService.MaximumPlotCount][];

        for (int plotIndex = 0; plotIndex < PlotLayoutConfigurationService.MaximumPlotCount; plotIndex++)
        {
            _referenceBarEnabledCheckBoxes[plotIndex] = new CheckBox[PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot];
            _referenceBarValueTextBoxes[plotIndex] = new TextBox[PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot];
            _referenceBarLabelTextBoxes[plotIndex] = new TextBox[PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot];

            _referenceBarPanels[plotIndex].Children.Clear();

            for (int referenceBarIndex = 0; referenceBarIndex < PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot; referenceBarIndex++)
            {
                CheckBox enabledCheckBox = new()
                {
                    Content = $"Bar {referenceBarIndex + 1}",
                    Width = 80
                };
                enabledCheckBox.IsCheckedChanged += ReferenceBarCheckBox_Changed;

                TextBox valueTextBox = new()
                {
                    Width = 110,
                    Watermark = "Value"
                };
                TextBox labelTextBox = new()
                {
                    Width = 220,
                    Watermark = "Label"
                };
                StackPanel row = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };
                row.Children.Add(enabledCheckBox);
                row.Children.Add(valueTextBox);
                row.Children.Add(labelTextBox);
                _referenceBarPanels[plotIndex].Children.Add(row);

                _referenceBarEnabledCheckBoxes[plotIndex][referenceBarIndex] = enabledCheckBox;
                _referenceBarValueTextBoxes[plotIndex][referenceBarIndex] = valueTextBox;
                _referenceBarLabelTextBoxes[plotIndex][referenceBarIndex] = labelTextBox;
            }
        }
    }

    private static bool TryParseDouble(string? value, out double result)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return double.IsFinite(result);
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) &&
            double.IsFinite(result);
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private sealed record TimeWindowOption(double? Seconds, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
