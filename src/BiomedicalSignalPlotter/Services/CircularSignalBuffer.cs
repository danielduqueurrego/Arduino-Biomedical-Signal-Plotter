using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Services;

public sealed class CircularSignalBuffer
{
    private readonly object _syncRoot = new();
    private readonly double[] _timeSeconds;
    private readonly double[] _channel1;
    private readonly double[] _channel2;
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
        _channel1 = new double[capacity];
        _channel2 = new double[capacity];
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
            _channel1[_nextIndex] = sample.Channel1;
            _channel2[_nextIndex] = sample.Channel2;

            _nextIndex = (_nextIndex + 1) % Capacity;
            _count = Math.Min(_count + 1, Capacity);
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            Array.Clear(_timeSeconds);
            Array.Clear(_channel1);
            Array.Clear(_channel2);
            _nextIndex = 0;
            _count = 0;
        }
    }

    public SignalSnapshot Snapshot()
    {
        lock (_syncRoot)
        {
            double[] timeSeconds = new double[_count];
            double[] channel1 = new double[_count];
            double[] channel2 = new double[_count];

            for (int i = 0; i < _count; i++)
            {
                int sourceIndex = (_nextIndex - _count + i + Capacity) % Capacity;
                timeSeconds[i] = _timeSeconds[sourceIndex];
                channel1[i] = _channel1[sourceIndex];
                channel2[i] = _channel2[sourceIndex];
            }

            return new SignalSnapshot(timeSeconds, channel1, channel2);
        }
    }
}
