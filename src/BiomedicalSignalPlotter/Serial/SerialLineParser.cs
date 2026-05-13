using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Serial;

public sealed class SerialLineParser
{
    public static bool IsCommentOrMetadataLine(string? line)
    {
        return !string.IsNullOrWhiteSpace(line) && line.TrimStart().StartsWith('#');
    }

    public bool TryParse(
        string? line,
        int expectedChannelCount,
        [NotNullWhen(true)] out SerialChannelValues? values)
    {
        AnalogChannelLimits.Validate(expectedChannelCount);
        values = null;

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
        if (parts.Length != expectedChannelCount)
        {
            return false;
        }

        double[] parsedValues = new double[expectedChannelCount];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!TryParseFiniteDouble(parts[i], out parsedValues[i]))
            {
                return false;
            }
        }

        values = new SerialChannelValues(parsedValues);
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
