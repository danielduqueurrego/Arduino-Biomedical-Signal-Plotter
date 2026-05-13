using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Services;

public sealed class CircularSignalBuffer
{
    private readonly object _syncRoot = new();
    private readonly double[] _timeSeconds;
    private readonly double[][] _channels;
    private int _nextIndex;
    private int _count;

    public CircularSignalBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        Capacity = capacity;
        _timeSeconds = new double[capacity];
        _channels = Enumerable.Range(0, AnalogChannelLimits.Maximum)
            .Select(_ => new double[capacity])
            .ToArray();
    }

    public int Capacity { get; }

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _count;
            }
        }
    }

    public void Add(SignalSample sample)
    {
        lock (_syncRoot)
        {
            _timeSeconds[_nextIndex] = sample.TimeSeconds;

            for (int channelIndex = 0; channelIndex < AnalogChannelLimits.Maximum; channelIndex++)
            {
                _channels[channelIndex][_nextIndex] = channelIndex < sample.ChannelCount
                    ? sample.Values[channelIndex]
                    : double.NaN;
            }

            _nextIndex = (_nextIndex + 1) % Capacity;
            _count = Math.Min(_count + 1, Capacity);
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            Array.Clear(_timeSeconds);
            foreach (double[] channel in _channels)
            {
                Array.Clear(channel);
            }

            _nextIndex = 0;
            _count = 0;
        }
    }

    public SignalSnapshot Snapshot(int channelCount)
    {
        AnalogChannelLimits.Validate(channelCount);

        lock (_syncRoot)
        {
            double[] timeSeconds = new double[_count];
            double[][] channels = Enumerable.Range(0, channelCount)
                .Select(_ => new double[_count])
                .ToArray();

            for (int i = 0; i < _count; i++)
            {
                int sourceIndex = (_nextIndex - _count + i + Capacity) % Capacity;
                timeSeconds[i] = _timeSeconds[sourceIndex];

                for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                {
                    channels[channelIndex][i] = _channels[channelIndex][sourceIndex];
                }
            }

            return new SignalSnapshot(timeSeconds, channels);
        }
    }
}
