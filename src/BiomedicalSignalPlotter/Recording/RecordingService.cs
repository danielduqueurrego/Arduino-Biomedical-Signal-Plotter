using System.Diagnostics;
using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Recording;

public sealed class RecordingService
{
    private readonly object _syncRoot = new();
    private readonly List<RecordedSample> _samples = [];
    private readonly Stopwatch _stopwatch = new();
    private double _elapsedBeforeCurrentSegmentSeconds;

    public bool IsRecording { get; private set; }

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _samples.Count;
            }
        }
    }

    public void Start()
    {
        lock (_syncRoot)
        {
            if (IsRecording)
            {
                return;
            }

            _stopwatch.Restart();
            IsRecording = true;
        }
    }

    public void Stop()
    {
        lock (_syncRoot)
        {
            if (!IsRecording)
            {
                return;
            }

            _elapsedBeforeCurrentSegmentSeconds += _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Stop();
            IsRecording = false;
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _samples.Clear();
            _elapsedBeforeCurrentSegmentSeconds = 0;

            if (IsRecording)
            {
                _stopwatch.Restart();
            }
            else
            {
                _stopwatch.Reset();
            }
        }
    }

    public void Record(SignalSample sample, RecordedSampleSource source, int channelCount)
    {
        AnalogChannelLimits.Validate(channelCount);

        lock (_syncRoot)
        {
            int targetChannelCount = _samples.Count > 0 ? _samples[0].ChannelCount : channelCount;

            if (!IsRecording || channelCount != targetChannelCount || sample.ChannelCount < targetChannelCount)
            {
                return;
            }

            double timeSeconds = _elapsedBeforeCurrentSegmentSeconds + _stopwatch.Elapsed.TotalSeconds;
            double[] values = new double[targetChannelCount];
            Array.Copy(sample.Values, values, targetChannelCount);
            _samples.Add(new RecordedSample(timeSeconds, values, source));
        }
    }

    public IReadOnlyList<RecordedSample> Snapshot()
    {
        lock (_syncRoot)
        {
            return _samples.ToArray();
        }
    }
}
