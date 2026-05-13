using System.Diagnostics;

namespace BiomedicalSignalPlotter.Arduino;

public sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new()
        {
            StartInfo = startInfo
        };

        process.Start();

        using CancellationTokenSource timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutTokenSource.CancelAfter(timeout);

        try
        {
            string standardOutput = await process.StandardOutput.ReadToEndAsync(timeoutTokenSource.Token).ConfigureAwait(false);
            string standardError = await process.StandardError.ReadToEndAsync(timeoutTokenSource.Token).ConfigureAwait(false);
            await process.WaitForExitAsync(timeoutTokenSource.Token).ConfigureAwait(false);

            return new CommandResult(process.ExitCode, standardOutput, standardError);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException($"Command '{fileName}' timed out after {timeout.TotalSeconds:0.#} seconds.");
        }
    }
}
