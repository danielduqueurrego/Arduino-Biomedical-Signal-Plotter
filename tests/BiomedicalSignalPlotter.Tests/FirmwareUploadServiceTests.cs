using System.ComponentModel;
using BiomedicalSignalPlotter.Arduino;

namespace BiomedicalSignalPlotter.Tests;

public class FirmwareUploadServiceTests
{
    private const string SketchPath = "firmware/arduino/TwoChannelCsvStreamer";

    [Fact]
    public async Task UploadUnoR4WifiAsync_ReturnsNotFoundWhenArduinoCliIsMissing()
    {
        FakeCommandRunner runner = new();
        runner.EnqueueException(new Win32Exception("not found"));
        FirmwareUploadService service = new(runner, SketchPath);

        FirmwareUploadResult result = await service.UploadUnoR4WifiAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Arduino CLI not found", result.Message);
        Assert.Single(runner.Calls);
        Assert.Equal(["version"], runner.Calls[0].Arguments);
    }

    [Fact]
    public async Task UploadUnoR4WifiAsync_ReturnsNoBoardWhenNoUnoIsDetected()
    {
        FakeCommandRunner runner = CreateRunnerWithVersion();
        runner.EnqueueResult(new CommandResult(0, EmptyBoardListJson, string.Empty));
        FirmwareUploadService service = new(runner, SketchPath);

        FirmwareUploadResult result = await service.UploadUnoR4WifiAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("No UNO R4 WiFi detected", result.Message);
    }

    [Fact]
    public async Task UploadUnoR4WifiAsync_ReturnsMultipleBoardsWhenMoreThanOneUnoIsDetected()
    {
        FakeCommandRunner runner = CreateRunnerWithVersion();
        runner.EnqueueResult(new CommandResult(0, BoardListJson("COM5", "COM6"), string.Empty));
        FirmwareUploadService service = new(runner, SketchPath);

        FirmwareUploadResult result = await service.UploadUnoR4WifiAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Multiple UNO R4 WiFi boards detected", result.Message);
        Assert.Contains("COM5", result.Message);
        Assert.Contains("COM6", result.Message);
    }

    [Fact]
    public async Task UploadUnoR4WifiAsync_ReturnsCompileFailure()
    {
        FakeCommandRunner runner = CreateRunnerWithVersionAndOneBoard();
        runner.EnqueueResult(new CommandResult(1, string.Empty, "compile failed"));
        FirmwareUploadService service = new(runner, SketchPath);

        FirmwareUploadResult result = await service.UploadUnoR4WifiAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("compiling firmware", result.Message);
        Assert.Contains("compile failed", result.Message);
    }

    [Fact]
    public async Task UploadUnoR4WifiAsync_ReturnsUploadFailure()
    {
        FakeCommandRunner runner = CreateRunnerWithVersionAndOneBoard();
        runner.EnqueueResult(new CommandResult(0, "compile ok", string.Empty));
        runner.EnqueueResult(new CommandResult(1, string.Empty, "upload failed"));
        FirmwareUploadService service = new(runner, SketchPath);

        FirmwareUploadResult result = await service.UploadUnoR4WifiAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Upload failed", result.Message);
        Assert.Contains("upload failed", result.Message);
    }

    [Fact]
    public async Task UploadUnoR4WifiAsync_RunsExpectedCommandsForSuccess()
    {
        FakeCommandRunner runner = CreateRunnerWithVersionAndOneBoard();
        runner.EnqueueResult(new CommandResult(0, "compile ok", string.Empty));
        runner.EnqueueResult(new CommandResult(0, "upload ok", string.Empty));
        List<string> progressMessages = [];
        FirmwareUploadService service = new(runner, SketchPath);

        FirmwareUploadResult result = await service.UploadUnoR4WifiAsync(new Progress<string>(progressMessages.Add));

        Assert.True(result.Succeeded);
        Assert.Equal("COM5", result.PortName);
        Assert.Contains("Upload succeeded", result.Message);
        Assert.Equal(
            [
                "version",
                "board list --json",
                $"compile --fqbn {FirmwareUploadService.UnoR4WifiFqbn} {SketchPath}",
                $"upload -p COM5 --fqbn {FirmwareUploadService.UnoR4WifiFqbn} {SketchPath}"
            ],
            runner.Calls.Select(call => string.Join(' ', call.Arguments)).ToArray());
    }

    [Fact]
    public void FindUnoR4WifiBoards_ReturnsOneMatchingBoard()
    {
        IReadOnlyList<ArduinoBoardInfo> boards = FirmwareUploadService.FindUnoR4WifiBoards(BoardListJson("COM5"));

        ArduinoBoardInfo board = Assert.Single(boards);
        Assert.Equal("COM5", board.PortName);
        Assert.Equal(FirmwareUploadService.UnoR4WifiFqbn, board.Fqbn);
        Assert.Equal(FirmwareUploadService.UnoR4WifiBoardName, board.Name);
    }

    private static FakeCommandRunner CreateRunnerWithVersion()
    {
        FakeCommandRunner runner = new();
        runner.EnqueueResult(new CommandResult(0, "arduino-cli Version: 1.4.1", string.Empty));
        return runner;
    }

    private static FakeCommandRunner CreateRunnerWithVersionAndOneBoard()
    {
        FakeCommandRunner runner = CreateRunnerWithVersion();
        runner.EnqueueResult(new CommandResult(0, BoardListJson("COM5"), string.Empty));
        return runner;
    }

    private static string BoardListJson(params string[] ports)
    {
        string detectedPorts = string.Join(
            ',',
            ports.Select(port => $$"""
            {
              "port": { "address": "{{port}}" },
              "matching_boards": [
                {
                  "name": "{{FirmwareUploadService.UnoR4WifiBoardName}}",
                  "fqbn": "{{FirmwareUploadService.UnoR4WifiFqbn}}"
                }
              ]
            }
            """));

        return $$"""
        {
          "detected_ports": [
            {{detectedPorts}}
          ]
        }
        """;
    }

    private const string EmptyBoardListJson = """
    {
      "detected_ports": []
    }
    """;

    private sealed class FakeCommandRunner : ICommandRunner
    {
        private readonly Queue<object> _responses = [];

        public List<CommandCall> Calls { get; } = [];

        public void EnqueueResult(CommandResult result)
        {
            _responses.Enqueue(result);
        }

        public void EnqueueException(Exception exception)
        {
            _responses.Enqueue(exception);
        }

        public Task<CommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new CommandCall(fileName, arguments.ToArray()));

            object response = _responses.Dequeue();
            if (response is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((CommandResult)response);
        }
    }

    private sealed record CommandCall(string FileName, IReadOnlyList<string> Arguments);
}
