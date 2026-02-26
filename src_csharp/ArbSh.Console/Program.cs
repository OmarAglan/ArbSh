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
                        ShellEngine.ExecuteInput(inputLine, sink, executionOptions);
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
    }
}
