using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Services;

public sealed class SimulatedDataService : IDisposable
{
    private const double TwoPi = 2.0 * Math.PI;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _generationTask;
    private long _sampleIndex;

    public event EventHandler<SignalSample>? SampleGenerated;

    public double SampleRateHz { get; }

    public bool IsRunning { get; private set; }

    public SimulatedDataService(double sampleRateHz = 200.0)
    {
        if (sampleRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRateHz), "Sample rate must be greater than zero.");
        }

        SampleRateHz = sampleRateHz;
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _generationTask = Task.Run(() => GenerateSamplesAsync(_cancellationTokenSource.Token));
        IsRunning = true;
    }

    public async Task StopAsync()
    {
        if (!IsRunning || _cancellationTokenSource is null || _generationTask is null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();

        try
        {
            await _generationTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping the simulation.
        }
        finally
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _generationTask = null;
            IsRunning = false;
        }
    }

    public static SignalSample CreateSample(long sampleIndex, double sampleRateHz)
    {
        if (sampleRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRateHz), "Sample rate must be greater than zero.");
        }

        double timeSeconds = sampleIndex / sampleRateHz;
        double beatPhase = timeSeconds % 1.0;

        double pWave = 20.0 * GaussianPulse(beatPhase, 0.15, 0.035);
        double qDip = -35.0 * GaussianPulse(beatPhase, 0.30, 0.012);
        double rPeak = 180.0 * GaussianPulse(beatPhase, 0.33, 0.014);
        double sDip = -45.0 * GaussianPulse(beatPhase, 0.36, 0.018);
        double tWave = 55.0 * GaussianPulse(beatPhase, 0.56, 0.075);
        double baseline = 18.0 * Math.Sin(TwoPi * 0.25 * timeSeconds);

        double channel1 = 520.0 + baseline + pWave + qDip + rPeak + sDip + tWave;
        double channel2 = 335.0
            + 55.0 * Math.Sin(TwoPi * 0.55 * timeSeconds - 0.7)
            + 14.0 * Math.Sin(TwoPi * 1.10 * timeSeconds);

        return new SignalSample(timeSeconds, channel1, channel2);
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    private async Task GenerateSamplesAsync(CancellationToken cancellationToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(1.0 / SampleRateHz);
        using PeriodicTimer timer = new(interval);

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            SignalSample sample = CreateSample(Interlocked.Increment(ref _sampleIndex), SampleRateHz);
            SampleGenerated?.Invoke(this, sample);
        }
    }

    private static double GaussianPulse(double phase, double center, double width)
    {
        double distance = phase - center;
        return Math.Exp(-(distance * distance) / (2.0 * width * width));
    }
}
