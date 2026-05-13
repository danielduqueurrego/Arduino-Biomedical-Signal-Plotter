using System.Globalization;

namespace BiomedicalSignalPlotter.Arduino;

public sealed record ArduinoStatusReport(
    int ChannelCount,
    int AdcBits,
    int SampleRateHz,
    bool Streaming)
{
    public bool Matches(ArduinoDeviceSettings settings)
    {
        return ChannelCount == settings.ChannelCount &&
            AdcBits == settings.AdcBits &&
            SampleRateHz == settings.SampleRateHz;
    }

    public static bool TryParse(string? line, out ArduinoStatusReport? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string trimmed = line.Trim();
        if (!trimmed.StartsWith("#STATUS ", StringComparison.Ordinal))
        {
            return false;
        }

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (string token in trimmed["#STATUS ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = token.Split('=', 2);
            if (parts.Length == 2)
            {
                values[parts[0]] = parts[1];
            }
        }

        if (!TryGetInt(values, "CHANNEL_COUNT", out int channelCount) ||
            !TryGetInt(values, "ADC_BITS", out int adcBits) ||
            !TryGetInt(values, "SAMPLE_RATE_HZ", out int sampleRateHz) ||
            !TryGetInt(values, "STREAMING", out int streamingValue))
        {
            return false;
        }

        status = new ArduinoStatusReport(channelCount, adcBits, sampleRateHz, streamingValue != 0);
        return true;
    }

    private static bool TryGetInt(IReadOnlyDictionary<string, string> values, string key, out int value)
    {
        value = 0;
        return values.TryGetValue(key, out string? text) &&
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
