using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Arduino;

public static class ArduinoDeviceSettingsLimits
{
    public const int MinimumAdcBits = 8;
    public const int MaximumAdcBits = 14;
    public const int DefaultAdcBits = 14;

    public const int MinimumSampleRateHz = 1;
    public const int MaximumSampleRateHz = 1_000;
    public const int DefaultSampleRateHz = 250;

    public static int ValidateChannelCount(int channelCount)
    {
        return AnalogChannelLimits.Validate(channelCount);
    }

    public static int ValidateAdcBits(int adcBits)
    {
        if (adcBits is < MinimumAdcBits or > MaximumAdcBits)
        {
            throw new ArgumentOutOfRangeException(
                nameof(adcBits),
                $"ADC bits must be between {MinimumAdcBits} and {MaximumAdcBits}.");
        }

        return adcBits;
    }

    public static int ValidateSampleRateHz(int sampleRateHz)
    {
        if (sampleRateHz is < MinimumSampleRateHz or > MaximumSampleRateHz)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleRateHz),
                $"Sample rate must be between {MinimumSampleRateHz} Hz and {MaximumSampleRateHz} Hz.");
        }

        return sampleRateHz;
    }

    public static int ClampAdcBits(int adcBits)
    {
        return Math.Clamp(adcBits, MinimumAdcBits, MaximumAdcBits);
    }

    public static int ClampSampleRateHz(int sampleRateHz)
    {
        return Math.Clamp(sampleRateHz, MinimumSampleRateHz, MaximumSampleRateHz);
    }
}
