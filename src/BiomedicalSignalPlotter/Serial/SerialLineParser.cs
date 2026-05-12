using System.Globalization;

namespace BiomedicalSignalPlotter.Serial;

public sealed class SerialLineParser
{
    public bool TryParse(string? line, out SerialChannelValues values)
    {
        values = default;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string trimmed = line.Trim();
        if (trimmed.StartsWith('#'))
        {
            return false;
        }

        string[] parts = trimmed.Split(',');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseFiniteDouble(parts[0], out double channel1) ||
            !TryParseFiniteDouble(parts[1], out double channel2))
        {
            return false;
        }

        values = new SerialChannelValues(channel1, channel2);
        return true;
    }

    private static bool TryParseFiniteDouble(string value, out double result)
    {
        bool parsed = double.TryParse(
            value.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);

        return parsed && double.IsFinite(result);
    }
}
