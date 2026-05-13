using System.ComponentModel;
using BiomedicalSignalPlotter.Arduino;

namespace BiomedicalSignalPlotter.Tests;

public class ArduinoCliServiceTests
{
    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsAvailableForSuccessfulVersionCommand()
    {
        FakeCommandRunner runner = new(new CommandResult(0, "arduino-cli Version: 1.3.1\n", string.Empty));
        ArduinoCliService service = new(runner);

        ArduinoCliCheckResult result = await service.CheckAvailabilityAsync();

        Assert.True(result.IsAvailable);
        Assert.Contains("Arduino CLI available", result.Message);
        Assert.Equal("arduino-cli", runner.FileName);
        Assert.Equal(["version"], runner.Arguments);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsUnavailableForMissingExecutable()
    {
        FakeCommandRunner runner = new(new Win32Exception("not found"));
        ArduinoCliService service = new(runner);

        ArduinoCliCheckResult result = await service.CheckAvailabilityAsync();

        Assert.False(result.IsAvailable);
        Assert.Contains("not for simulation", result.Message);
    }

    private sealed class FakeCommandRunner : ICommandRunner
    {
        private readonly CommandResult? _result;
        private readonly Exception? _exception;

        public FakeCommandRunner(CommandResult result)
        {
            _result = result;
        }

        public FakeCommandRunner(Exception exception)
        {
            _exception = exception;
        }

        public string? FileName { get; private set; }

        public IReadOnlyList<string> Arguments { get; private set; } = [];

        public Task<CommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            FileName = fileName;
            Arguments = arguments.ToArray();

            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_result!);
        }
    }
}
