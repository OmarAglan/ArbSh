using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using ArbSh.Core.Processes;

namespace ArbSh.Test;

public sealed class ExternalProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_PreservesArabicPathsArgumentsEnvironmentAndInput()
    {
        using var fixture = FixtureCopy.Create();
        string workingDirectory = Path.Combine(fixture.RootDirectory, "مجلد عمل عربي");
        Directory.CreateDirectory(workingDirectory);
        string[] logicalArguments = [string.Empty, "وسيط عربي", "value with spaces", "; & | < >"];
        const string environmentValue = "قيمة بيئية عربية";
        const string standardInput = "مدخل عربي\nسطر ثان";
        var runner = new SystemExternalProcessRunner();
        var request = new ExternalProcessRequest(
            ResolveDotNetHost(),
            [fixture.AssemblyPath, "inspect", .. logicalArguments],
            workingDirectory,
            new Dictionary<string, string?>
            {
                ["ARBSH_PHASE2_VALUE"] = environmentValue
            },
            standardInputMode: ExternalProcessInputMode.Text,
            standardInputText: standardInput);

        ExternalProcessResult result = await runner.RunAsync(request);

        Assert.Equal(ExternalProcessTerminationReason.Exited, result.TerminationReason);
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.Succeeded);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Null(result.FailureMessage);

        InspectionPayload payload = Assert.IsType<InspectionPayload>(
            JsonSerializer.Deserialize<InspectionPayload>(result.StandardOutput));
        Assert.Equal(logicalArguments, payload.Arguments);
        Assert.Equal(Path.GetFullPath(workingDirectory), Path.GetFullPath(payload.WorkingDirectory));
        Assert.Equal(environmentValue, payload.EnvironmentValue);
        Assert.Equal(standardInput, payload.StandardInput);
    }

    [Fact]
    public async Task RunAsync_CapturesSeparateStreamsAndPreservesExitCode()
    {
        using var fixture = FixtureCopy.Create();
        var runner = new SystemExternalProcessRunner();
        var request = new ExternalProcessRequest(
            ResolveDotNetHost(),
            [fixture.AssemblyPath, "streams", "خرج عربي", "خطأ عربي", "37"]);

        ExternalProcessResult result = await runner.RunAsync(request);

        Assert.Equal(ExternalProcessTerminationReason.Exited, result.TerminationReason);
        Assert.Equal(37, result.ExitCode);
        Assert.False(result.Succeeded);
        Assert.Equal("خرج عربي", result.StandardOutput);
        Assert.Equal("خطأ عربي", result.StandardError);
        Assert.Null(result.FailureMessage);
    }

    [Fact]
    public async Task RunAsync_DefaultInputModeClosesStandardInput()
    {
        using var fixture = FixtureCopy.Create();
        var runner = new SystemExternalProcessRunner();
        var request = new ExternalProcessRequest(
            ResolveDotNetHost(),
            [fixture.AssemblyPath, "stdin-length"]);

        ExternalProcessResult result = await runner.RunAsync(request);

        Assert.Equal(ExternalProcessTerminationReason.Exited, result.TerminationReason);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_CanStartWithCleanEnvironmentAndApplyExplicitValues()
    {
        using var fixture = FixtureCopy.Create();
        string inheritedName = $"ARBSH_PHASE2_PARENT_{Guid.NewGuid():N}";
        string explicitName = $"ARBSH_PHASE2_CHILD_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(inheritedName, "parent-only");

        try
        {
            var runner = new SystemExternalProcessRunner();
            var request = new ExternalProcessRequest(
                ResolveDotNetHost(),
                [fixture.AssemblyPath, "environment", inheritedName, explicitName],
                environmentChanges: new Dictionary<string, string?>
                {
                    [explicitName] = "قيمة صريحة"
                },
                inheritEnvironment: false);

            ExternalProcessResult result = await runner.RunAsync(request);
            string?[] values = Assert.IsType<string?[]>(
                JsonSerializer.Deserialize<string?[]>(result.StandardOutput));

            Assert.Equal(ExternalProcessTerminationReason.Exited, result.TerminationReason);
            Assert.Equal(0, result.ExitCode);
            Assert.Collection(
                values,
                value => Assert.Null(value),
                value => Assert.Equal("قيمة صريحة", value));
        }
        finally
        {
            Environment.SetEnvironmentVariable(inheritedName, null);
        }
    }

    [Fact]
    public async Task RunAsync_NullEnvironmentChangeRemovesInheritedValue()
    {
        using var fixture = FixtureCopy.Create();
        string variableName = $"ARBSH_PHASE2_REMOVE_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(variableName, "remove-me");

        try
        {
            var runner = new SystemExternalProcessRunner();
            var request = new ExternalProcessRequest(
                ResolveDotNetHost(),
                [fixture.AssemblyPath, "environment", variableName],
                environmentChanges: new Dictionary<string, string?>
                {
                    [variableName] = null
                });

            ExternalProcessResult result = await runner.RunAsync(request);
            string?[] values = Assert.IsType<string?[]>(
                JsonSerializer.Deserialize<string?[]>(result.StandardOutput));

            Assert.Equal(ExternalProcessTerminationReason.Exited, result.TerminationReason);
            Assert.Collection(values, value => Assert.Null(value));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Fact]
    public async Task RunAsync_MissingExecutableReturnsLaunchFailure()
    {
        var runner = new SystemExternalProcessRunner();
        string missingExecutable = Path.Combine(
            Path.GetTempPath(),
            $"أربش-غير-موجود-{Guid.NewGuid():N}",
            "برنامج مفقود");
        var request = new ExternalProcessRequest(missingExecutable);

        ExternalProcessResult result = await runner.RunAsync(request);

        Assert.Equal(ExternalProcessTerminationReason.LaunchFailed, result.TerminationReason);
        Assert.Null(result.ExitCode);
        Assert.False(result.Succeeded);
        Assert.Equal(string.Empty, result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureMessage));
    }

    [Fact]
    public async Task RunAsync_CancellationTerminatesTheProcess()
    {
        using var fixture = FixtureCopy.Create();
        string readyFile = Path.Combine(fixture.RootDirectory, "جاهز.txt");
        var runner = new SystemExternalProcessRunner();
        var request = new ExternalProcessRequest(
            ResolveDotNetHost(),
            [fixture.AssemblyPath, "wait", readyFile]);
        using var cancellation = new CancellationTokenSource();

        Task<ExternalProcessResult> runningProcess = runner.RunAsync(request, cancellation.Token);
        await WaitForFileAsync(readyFile, TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        ExternalProcessResult result = await runningProcess.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(ExternalProcessTerminationReason.Cancelled, result.TerminationReason);
        Assert.Null(result.ExitCode);
        Assert.False(result.Succeeded);
        Assert.Equal("ready", result.StandardOutput);
        Assert.Null(result.FailureMessage);
    }

    [Fact]
    public async Task RunAsync_AlreadyCancelledDoesNotLaunch()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var runner = new SystemExternalProcessRunner();
        var request = new ExternalProcessRequest("برنامج-لن-يعمل");

        ExternalProcessResult result = await runner.RunAsync(request, cancellation.Token);

        Assert.Equal(ExternalProcessTerminationReason.Cancelled, result.TerminationReason);
        Assert.Null(result.ExitCode);
        Assert.Null(result.FailureMessage);
    }

    [Fact]
    public void Request_RejectsInputTextOutsideTextMode()
    {
        Assert.Throws<ArgumentException>(() => new ExternalProcessRequest(
            "برنامج",
            standardInputText: "نص غير مسموح"));
    }

    [Fact]
    public void Request_SnapshotsArgumentsAndEnvironmentChanges()
    {
        var arguments = new List<string> { "الأول" };
        var environment = new Dictionary<string, string?>
        {
            ["ARBSH_PHASE2_SNAPSHOT"] = "قبل"
        };
        var request = new ExternalProcessRequest(
            "برنامج",
            arguments,
            environmentChanges: environment);

        arguments[0] = "الثاني";
        environment["ARBSH_PHASE2_SNAPSHOT"] = "بعد";

        Assert.Equal(["الأول"], request.Arguments);
        Assert.Equal("قبل", request.EnvironmentChanges["ARBSH_PHASE2_SNAPSHOT"]);
    }

    private static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!File.Exists(path))
        {
            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException($"لم تنشئ العملية المساعدة ملف الجاهزية: {path}");
            }

            await Task.Delay(25);
        }
    }

    private static string ResolveDotNetHost()
    {
        string? configuredHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(configuredHost) && File.Exists(configuredHost))
        {
            return configuredHost;
        }

        string hostName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        string dotNetRoot = Path.GetFullPath(Path.Combine(
            RuntimeEnvironment.GetRuntimeDirectory(),
            "..",
            "..",
            ".."));
        string hostPath = Path.Combine(dotNetRoot, hostName);
        return File.Exists(hostPath) ? hostPath : hostName;
    }

    private sealed class FixtureCopy : IDisposable
    {
        private FixtureCopy(string rootDirectory, string assemblyPath)
        {
            RootDirectory = rootDirectory;
            AssemblyPath = assemblyPath;
        }

        public string RootDirectory { get; }

        public string AssemblyPath { get; }

        public static FixtureCopy Create()
        {
            var testOutputDirectory = new DirectoryInfo(AppContext.BaseDirectory);
            string targetFramework = testOutputDirectory.Name;
            string configuration = testOutputDirectory.Parent?.Name
                ?? throw new InvalidOperationException("تعذر تحديد إعداد بناء الاختبارات.");
            string sourceRoot = testOutputDirectory.Parent?.Parent?.Parent?.Parent?.FullName
                ?? throw new InvalidOperationException("تعذر تحديد مجلد مشاريع ArbSh.");
            string fixtureOutputDirectory = Path.Combine(
                sourceRoot,
                "ArbSh.ProcessFixture",
                "bin",
                configuration,
                targetFramework);
            string rootDirectory = Path.Combine(
                Path.GetTempPath(),
                $"أربش مرحلة 2 {Guid.NewGuid():N}");
            Directory.CreateDirectory(rootDirectory);

            foreach (string sourcePath in Directory.EnumerateFiles(
                fixtureOutputDirectory,
                "ArbSh.ProcessFixture.*",
                SearchOption.TopDirectoryOnly))
            {
                string destinationPath = Path.Combine(rootDirectory, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, destinationPath);
            }

            string assemblyPath = Path.Combine(rootDirectory, "ArbSh.ProcessFixture.dll");
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("تعذر تجهيز العملية المساعدة للاختبارات.", assemblyPath);
            }

            return new FixtureCopy(rootDirectory, assemblyPath);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
            catch
            {
                // لا ينبغي أن يحجب فشل تنظيف مجلد مؤقت نتيجة الاختبار الأساسية.
            }
        }
    }

    private sealed record InspectionPayload(
        string[] Arguments,
        string WorkingDirectory,
        string? EnvironmentValue,
        string StandardInput);
}
