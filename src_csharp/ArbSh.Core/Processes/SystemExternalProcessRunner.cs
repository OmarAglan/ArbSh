using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace ArbSh.Core.Processes;

/// <summary>
/// تنفيذ نظامي للعملية المنظمة باستخدام <see cref="ProcessStartInfo.ArgumentList"/> مباشرة.
/// </summary>
public sealed class SystemExternalProcessRunner : IExternalProcessRunner
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: false);

    /// <inheritdoc />
    public async Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        if (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return CreateCancelledResult(stopwatch.Elapsed, string.Empty, string.Empty);
        }

        using var process = new Process
        {
            StartInfo = CreateStartInfo(request)
        };

        try
        {
            if (!process.Start())
            {
                stopwatch.Stop();
                return CreateLaunchFailure(stopwatch.Elapsed, "رفض النظام بدء العملية دون تقديم تفاصيل إضافية.");
            }
        }
        catch (Exception exception) when (IsLaunchException(exception))
        {
            stopwatch.Stop();
            return CreateLaunchFailure(stopwatch.Elapsed, exception.Message);
        }

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
        Task standardInputTask = WriteStandardInputAsync(process, request);
        bool cancelled = false;

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
            TerminateProcess(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        await ObserveStandardInputAsync(standardInputTask, process, cancelled).ConfigureAwait(false);
        string standardOutput = await standardOutputTask.ConfigureAwait(false);
        string standardError = await standardErrorTask.ConfigureAwait(false);
        stopwatch.Stop();

        if (cancelled)
        {
            return CreateCancelledResult(stopwatch.Elapsed, standardOutput, standardError);
        }

        return new ExternalProcessResult(
            ExternalProcessTerminationReason.Exited,
            process.ExitCode,
            standardOutput,
            standardError,
            stopwatch.Elapsed,
            failureMessage: null);
    }

    private static ProcessStartInfo CreateStartInfo(ExternalProcessRequest request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.Executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = request.StandardInputMode is not ExternalProcessInputMode.Inherit,
            StandardOutputEncoding = Utf8WithoutBom,
            StandardErrorEncoding = Utf8WithoutBom
        };

        if (startInfo.RedirectStandardInput)
        {
            startInfo.StandardInputEncoding = Utf8WithoutBom;
        }

        if (request.WorkingDirectory is not null)
        {
            startInfo.WorkingDirectory = request.WorkingDirectory;
        }

        foreach (string argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!request.InheritEnvironment)
        {
            startInfo.Environment.Clear();
        }

        foreach (KeyValuePair<string, string?> change in request.EnvironmentChanges)
        {
            if (change.Value is null)
            {
                startInfo.Environment.Remove(change.Key);
            }
            else
            {
                startInfo.Environment[change.Key] = change.Value;
            }
        }

        return startInfo;
    }

    private static async Task WriteStandardInputAsync(
        Process process,
        ExternalProcessRequest request)
    {
        if (request.StandardInputMode is ExternalProcessInputMode.Inherit)
        {
            return;
        }

        try
        {
            if (request.StandardInputMode is ExternalProcessInputMode.Text)
            {
                await process.StandardInput.WriteAsync(request.StandardInputText).ConfigureAwait(false);
                await process.StandardInput.FlushAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            process.StandardInput.Close();
        }
    }

    private static async Task ObserveStandardInputAsync(
        Task standardInputTask,
        Process process,
        bool cancelled)
    {
        try
        {
            await standardInputTask.ConfigureAwait(false);
        }
        catch (Exception exception) when (
            (cancelled || process.HasExited)
            && exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // خروج العملية أو إلغاؤها قد يغلق الأنبوب قبل انتهاء الكاتب.
        }
    }

    private static void TerminateProcess(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) when (process.HasExited)
        {
            // انتهت العملية بالتزامن مع طلب الإلغاء.
        }
    }

    private static bool IsLaunchException(Exception exception)
    {
        return exception is Win32Exception
            or FileNotFoundException
            or DirectoryNotFoundException
            or UnauthorizedAccessException
            or InvalidOperationException
            or PlatformNotSupportedException;
    }

    private static ExternalProcessResult CreateLaunchFailure(TimeSpan duration, string message)
    {
        return new ExternalProcessResult(
            ExternalProcessTerminationReason.LaunchFailed,
            exitCode: null,
            standardOutput: string.Empty,
            standardError: string.Empty,
            duration,
            message);
    }

    private static ExternalProcessResult CreateCancelledResult(
        TimeSpan duration,
        string standardOutput,
        string standardError)
    {
        return new ExternalProcessResult(
            ExternalProcessTerminationReason.Cancelled,
            exitCode: null,
            standardOutput,
            standardError,
            duration,
            failureMessage: null);
    }
}
