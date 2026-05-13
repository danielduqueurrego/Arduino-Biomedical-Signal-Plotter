namespace BiomedicalSignalPlotter.Arduino;

public sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
