using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BiomedicalSignalPlotter.Models;
using BiomedicalSignalPlotter.Services;

namespace BiomedicalSignalPlotter;

public partial class MainWindow : Window
{
    private const int BufferCapacity = 2_000;
    private readonly CircularSignalBuffer _signalBuffer = new(BufferCapacity);
    private readonly SimulatedDataService _simulatedDataService = new();
    private readonly DispatcherTimer _plotRefreshTimer;
    private bool _serialPlaceholderConnected;

    public MainWindow()
    {
        InitializeComponent();

        ConfigurePlot();
        SignalPlot.Refresh();
        _simulatedDataService.SampleGenerated += SimulatedDataService_SampleGenerated;

        _plotRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _plotRefreshTimer.Tick += PlotRefreshTimer_Tick;
        _plotRefreshTimer.Start();
        StartSimulation();

        Closed += MainWindow_Closed;
    }

    private void ConfigurePlot()
    {
        SignalPlot.Plot.Title("Simulated two-channel data");
        SignalPlot.Plot.XLabel("Time (s)");
        SignalPlot.Plot.YLabel("ADC counts");
        SignalPlot.Plot.ShowLegend();
    }

    private void SimulatedDataService_SampleGenerated(object? sender, SignalSample sample)
    {
        _signalBuffer.Add(sample);
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
    }

    private void ConnectButton_Click(object? sender, RoutedEventArgs e)
    {
        _serialPlaceholderConnected = !_serialPlaceholderConnected;
        StatusText.Text = _serialPlaceholderConnected
            ? "Serial connection placeholder enabled. Real serial reading is not implemented yet."
            : "Serial connection placeholder disabled. Real serial reading is not implemented yet.";
    }

    private async void SimulationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_simulatedDataService.IsRunning)
        {
            await StopSimulationAsync();
            return;
        }

        StartSimulation();
    }

    private void StartSimulation()
    {
        _signalBuffer.Clear();
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

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _plotRefreshTimer.Stop();
        await _simulatedDataService.StopAsync();
    }
}
