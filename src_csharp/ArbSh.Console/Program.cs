using System;
using System.Linq;
using System.Text;
using ArbSh.Core;
using ArbSh.Console.I18n;

namespace ArbSh.Console
{
    internal static class Program
    {
        private const string ExitCommand = "اخرج";

        private static void Main(string[] args)
        {
            if (ConsoleEnvironment.TryLaunchInBetterTerminal(args))
            {
                return;
            }

            System.Console.InputEncoding = Encoding.UTF8;
            System.Console.OutputEncoding = Encoding.UTF8;

            var sink = new ConsoleExecutionSink();
            var session = new ShellSessionState(ResolveInitialWorkingDirectory(args, sink));
            var executionOptions = new ExecutionOptions
            {
                EmitDebug = args.Contains("--debug-console", StringComparer.OrdinalIgnoreCase)
            };

            ConsoleRTLDisplay.DisplayRTLText("مرحباً بكم في أربش (النموذج الأولي)!", rightAlign: true);
            ConsoleRTLDisplay.DisplayRTLText("اكتب 'اخرج' للخروج.", rightAlign: true);
            System.Console.WriteLine();

            if (executionOptions.EmitDebug)
            {
                ConsoleEnvironment.DisplayConsoleInfo();
                System.Console.WriteLine(ArabicConsoleInput.GetInputInfo());
                System.Console.WriteLine();
            }
            else
            {
                var consoleInfo = ConsoleEnvironment.DetectConsoleEnvironment();
                if (consoleInfo.ArabicSupport == ConsoleEnvironment.ArabicSupportLevel.Poor)
                {
                    ConsoleRTLDisplay.DisplayRTLText("⚠️  تحذير: المحطة الحالية لا تدعم النص العربي بشكل جيد", rightAlign: true);
                    ConsoleRTLDisplay.DisplayRTLText("💡 نصيحة: استخدم Windows Terminal للحصول على أفضل عرض للنص العربي", rightAlign: true);
                    System.Console.WriteLine();
                }
            }

            ArabicConsoleInput.Initialize(ArabicConsoleInput.InputStrategy.Auto);

            try
            {
                while (true)
                {
                    string? inputLine;

                    if (!System.Console.IsInputRedirected)
                    {
                        // Read RTL interactively while preserving logical order for parser/executor.
                        inputLine = RTLConsoleInput.ReadRTLLine();
                    }
                    else
                    {
                        inputLine = ArabicConsoleInput.ReadLine();
                    }

                    if (inputLine is null)
                    {
                        break;
                    }

                    inputLine = inputLine.Trim();
                    if (string.IsNullOrWhiteSpace(inputLine))
                    {
                        continue;
                    }

                    if (string.Equals(inputLine, ExitCommand, StringComparison.Ordinal))
                    {
                        break;
                    }

                    try
                    {
                        ShellEngine.ExecuteInput(inputLine, sink, executionOptions, session);
                    }
                    catch (NotImplementedException ex)
                    {
                        sink.WriteWarning($"Feature not implemented: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        sink.WriteError($"ERROR: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                sink.WriteError($"FATAL ERROR: {ex.Message}");
            }
            finally
            {
                ArabicConsoleInput.Cleanup();
                if (!System.Console.IsInputRedirected)
                {
                    System.Console.WriteLine("تم إغلاق أربش.");
                }
            }
        }

        private static string? ResolveInitialWorkingDirectory(string[] args, ConsoleExecutionSink sink)
        {
            if (!TryGetWorkingDirectoryArgument(args, out string? requestedPath))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                return null;
            }

            try
            {
                string fullPath = System.IO.Path.GetFullPath(requestedPath);
                if (System.IO.Directory.Exists(fullPath))
                {
                    return fullPath;
                }

                sink.WriteWarning($"تحذير: المجلد الابتدائي غير موجود: {requestedPath}");
                return null;
            }
            catch (Exception ex)
            {
                sink.WriteWarning($"تحذير: تعذّر استخدام المجلد الابتدائي '{requestedPath}': {ex.Message}");
                return null;
            }
        }

        private static bool TryGetWorkingDirectoryArgument(string[] args, out string? path)
        {
            path = null;
            if (args.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("--working-dir=", StringComparison.OrdinalIgnoreCase))
                {
                    path = arg.Substring("--working-dir=".Length).Trim();
                    return true;
                }

                if (string.Equals(arg, "--working-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    path = args[i + 1];
                    return true;
                }
            }

            return false;
        }
    }
}
