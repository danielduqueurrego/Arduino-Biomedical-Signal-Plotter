namespace BiomedicalSignalPlotter.Configuration;

public sealed record SignalModePreset(SignalMode Mode, string DisplayName, SignalConfiguration Configuration)
{
    public override string ToString() => DisplayName;
}
