using System;
using System.Text;
using ArbSh.Console.I18n;

namespace ArbSh.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (ConsoleEnvironment.TryLaunchInBetterTerminal(args)) return;

            System.Console.InputEncoding = System.Text.Encoding.UTF8;
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            ConsoleRTLDisplay.DisplayRTLText("مرحباً بكم في أربش (النموذج الأولي)!", rightAlign: true);
            ConsoleRTLDisplay.DisplayRTLText("اكتب 'exit' للخروج.", rightAlign: true);
            System.Console.WriteLine();

            if (args.Length > 0 && args[0] == "--debug-console")
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
                        // Use Custom RTL Input Loop
                        // Note: ReadRTLLine handles the prompt display internally now to keep it synced
                        inputLine = RTLConsoleInput.ReadRTLLine();
                    }
                    else
                    {
                        inputLine = ArabicConsoleInput.ReadLine();
                    }

                    if (inputLine == null) break;

                    inputLine = inputLine.Trim();
                    if (string.IsNullOrWhiteSpace(inputLine)) continue;

                    if (inputLine.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                    try
                    {
                        var commands = Parser.Parse(inputLine);
                        Executor.Execute(commands);
                    }
                    catch (NotImplementedException nie)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        string warningMessage = $"Feature not implemented: {nie.Message}";
                        System.Console.WriteLine(BiDiTextProcessor.ProcessOutputForDisplay(warningMessage));
                        System.Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        string errorMessage = $"ERROR: {ex.Message}";
                        System.Console.WriteLine(BiDiTextProcessor.ProcessOutputForDisplay(errorMessage));
                        System.Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"FATAL ERROR: {ex.Message}");
                System.Console.ResetColor();
            }
            finally
            {
                ArabicConsoleInput.Cleanup();
                if (!System.Console.IsInputRedirected) System.Console.WriteLine("Exiting ArbSh.");
            }
        }
    }
}