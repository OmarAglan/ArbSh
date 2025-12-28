using System;
using System.Text; // Required for Encoding
using ArbSh.Console.I18n; // For Arabic console input support

namespace ArbSh.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check console environment and try to launch in better terminal if needed
            if (ConsoleEnvironment.TryLaunchInBetterTerminal(args))
            {
                return;
            }

            System.Console.InputEncoding = System.Text.Encoding.UTF8;
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            ConsoleRTLDisplay.DisplayRTLText("مرحباً بكم في أربش (النموذج الأولي)!", rightAlign: true);
            ConsoleRTLDisplay.DisplayRTLText("اكتب 'exit' للخروج.", rightAlign: true);

            // Display console environment information
            if (args.Length > 0 && args[0] == "--debug-console")
            {
                ConsoleEnvironment.DisplayConsoleInfo();
                ConsoleRTLDisplay.DisplayRTLText(ArabicConsoleInput.GetInputInfo(), rightAlign: true);
                System.Console.WriteLine();
            }
            else
            {
                var consoleInfo = ConsoleEnvironment.DetectConsoleEnvironment();
                if (consoleInfo.ArabicSupport == ConsoleEnvironment.ArabicSupportLevel.Poor)
                {
                    System.Console.WriteLine("⚠️  تحذير: المحطة الحالية لا تدعم النص العربي بشكل جيد");
                    System.Console.WriteLine("💡 نصيحة: استخدم Windows Terminal للحصول على أفضل عرض للنص العربي");
                    System.Console.WriteLine();
                }
            }

            ArabicConsoleInput.Initialize(ArabicConsoleInput.InputStrategy.Auto);

            try
            {
                while (true)
                {
                    if (!System.Console.IsInputRedirected)
                    {
                        // Use the shared RTL display logic for the prompt
                        ConsoleRTLDisplay.DisplayRTLPrompt("أربش> ", forceRTL: true);
                    }

                    string? inputLine = ArabicConsoleInput.ReadLine();

                    if (inputLine == null)
                    {
                        if (!System.Console.IsInputRedirected) System.Console.WriteLine();
                        break;
                    }

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