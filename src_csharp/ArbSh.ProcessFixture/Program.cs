using System.Text;
using System.Text.Json;

namespace ArbSh.ProcessFixture;

/// <summary>
/// علامة عامة تمكّن الاختبارات من تحديد ناتج مشروع العملية المساعدة.
/// </summary>
public static class ProcessFixtureMarker;

internal static class Program
{
    public static async Task<int> Main(string[] arguments)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        if (arguments.Length == 0)
        {
            Console.Error.Write("missing fixture command");
            return 2;
        }

        return arguments[0] switch
        {
            "inspect" => await InspectAsync(arguments[1..]).ConfigureAwait(false),
            "streams" => WriteStreams(arguments[1..]),
            "environment" => WriteEnvironment(arguments[1..]),
            "wait" => await WaitAsync(arguments[1..]).ConfigureAwait(false),
            "stdin-length" => await WriteStandardInputLengthAsync().ConfigureAwait(false),
            _ => UnknownCommand(arguments[0])
        };
    }

    private static async Task<int> InspectAsync(string[] arguments)
    {
        string standardInput = await Console.In.ReadToEndAsync().ConfigureAwait(false);
        var payload = new InspectionPayload(
            arguments,
            Environment.CurrentDirectory,
            Environment.GetEnvironmentVariable("ARBSH_PHASE2_VALUE"),
            standardInput);

        Console.Out.Write(JsonSerializer.Serialize(payload));
        return 0;
    }

    private static int WriteStreams(string[] arguments)
    {
        if (arguments.Length != 3 || !int.TryParse(arguments[2], out int exitCode))
        {
            return 2;
        }

        Console.Out.Write(arguments[0]);
        Console.Error.Write(arguments[1]);
        return exitCode;
    }

    private static int WriteEnvironment(string[] variableNames)
    {
        string?[] values = variableNames
            .Select(Environment.GetEnvironmentVariable)
            .ToArray();
        Console.Out.Write(JsonSerializer.Serialize(values));
        return 0;
    }

    private static async Task<int> WaitAsync(string[] arguments)
    {
        if (arguments.Length != 1)
        {
            return 2;
        }

        await File.WriteAllTextAsync(arguments[0], "جاهز", Encoding.UTF8).ConfigureAwait(false);
        Console.Out.Write("ready");
        Console.Out.Flush();
        await Task.Delay(Timeout.InfiniteTimeSpan).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> WriteStandardInputLengthAsync()
    {
        string standardInput = await Console.In.ReadToEndAsync().ConfigureAwait(false);
        Console.Out.Write(standardInput.Length);
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.Write($"unknown fixture command: {command}");
        return 2;
    }

    private sealed record InspectionPayload(
        string[] Arguments,
        string WorkingDirectory,
        string? EnvironmentValue,
        string StandardInput);
}
