using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Handles console environment detection and Arabic-friendly terminal launching.
    /// Addresses the issue where running ArbSh.exe opens in PowerShell which doesn't support Arabic properly.
    /// </summary>
    public static class ConsoleEnvironment
    {
        #region Console Environment Detection
        
        /// <summary>
        /// Detects the current console environment and its Arabic support capabilities.
        /// </summary>
        /// <returns>Information about the current console environment</returns>
        public static ConsoleInfo DetectConsoleEnvironment()
        {
            var info = new ConsoleInfo();
            
            try
            {
                // Check if we're running in Windows Terminal
                string? wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
                info.IsWindowsTerminal = !string.IsNullOrEmpty(wtSession);
                
                // Check if we're running in PowerShell
                string? psModulePath = Environment.GetEnvironmentVariable("PSModulePath");
                info.IsPowerShell = !string.IsNullOrEmpty(psModulePath);
                
                // Check console host process
                info.ConsoleHost = GetConsoleHostName();
                
                // Determine Arabic support level
                info.ArabicSupport = DetermineArabicSupport(info);
                
                // Check if input/output is redirected
                info.IsInputRedirected = System.Console.IsInputRedirected;
                info.IsOutputRedirected = System.Console.IsOutputRedirected;
                
                return info;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"WARNING: Console environment detection failed: {ex.Message}");
                return new ConsoleInfo { ArabicSupport = ArabicSupportLevel.Unknown };
            }
        }

        /// <summary>
        /// Attempts to launch ArbSh in a more Arabic-friendly terminal if possible.
        /// </summary>
        /// <param name="currentArgs">Current command line arguments</param>
        /// <returns>True if relaunched in better terminal, false if should continue in current terminal</returns>
        public static bool TryLaunchInBetterTerminal(string[] currentArgs)
        {
            var consoleInfo = DetectConsoleEnvironment();
            
            // If we're already in a good environment, don't relaunch
            if (consoleInfo.ArabicSupport == ArabicSupportLevel.Good)
            {
                return false;
            }
            
            // If input/output is redirected, don't relaunch
            if (consoleInfo.IsInputRedirected || consoleInfo.IsOutputRedirected)
            {
                return false;
            }
            
            // Try to launch in Windows Terminal if available
            if (TryLaunchInWindowsTerminal(currentArgs))
            {
                return true;
            }
            
            // Try to launch in Command Prompt (better than PowerShell for Arabic)
            if (consoleInfo.IsPowerShell && TryLaunchInCommandPrompt(currentArgs))
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Displays console environment information for debugging.
        /// </summary>
        public static void DisplayConsoleInfo()
        {
            var info = DetectConsoleEnvironment();
            
            System.Console.WriteLine("=== Console Environment Information ===");
            System.Console.WriteLine($"Console Host: {info.ConsoleHost}");
            System.Console.WriteLine($"Windows Terminal: {info.IsWindowsTerminal}");
            System.Console.WriteLine($"PowerShell: {info.IsPowerShell}");
            System.Console.WriteLine($"Arabic Support: {info.ArabicSupport}");
            System.Console.WriteLine($"Input Redirected: {info.IsInputRedirected}");
            System.Console.WriteLine($"Output Redirected: {info.IsOutputRedirected}");
            System.Console.WriteLine();
            
            // Provide recommendations
            if (info.ArabicSupport == ArabicSupportLevel.Poor)
            {
                System.Console.WriteLine("⚠️  WARNING: Current terminal has poor Arabic support!");
                System.Console.WriteLine("💡 RECOMMENDATION: Use Windows Terminal or Command Prompt for better Arabic display.");
                System.Console.WriteLine();
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Gets the name of the console host process.
        /// </summary>
        /// <returns>Console host name</returns>
        private static string GetConsoleHostName()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var parentProcess = GetParentProcess(currentProcess);
                return parentProcess?.ProcessName ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the parent process of the specified process.
        /// </summary>
        /// <param name="process">Process to get parent for</param>
        /// <returns>Parent process or null</returns>
        private static Process? GetParentProcess(Process process)
        {
            try
            {
                var parentPid = GetParentProcessId(process.Id);
                return parentPid > 0 ? Process.GetProcessById(parentPid) : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the parent process ID using Windows API.
        /// </summary>
        /// <param name="processId">Process ID</param>
        /// <returns>Parent process ID</returns>
        private static int GetParentProcessId(int processId)
        {
            // This is a simplified implementation
            // In a full implementation, you'd use Windows API calls
            return 0;
        }

        /// <summary>
        /// Determines the Arabic support level of the current console environment.
        /// </summary>
        /// <param name="info">Console information</param>
        /// <returns>Arabic support level</returns>
        private static ArabicSupportLevel DetermineArabicSupport(ConsoleInfo info)
        {
            if (info.IsWindowsTerminal)
            {
                return ArabicSupportLevel.Good;
            }
            
            if (info.IsPowerShell)
            {
                return ArabicSupportLevel.Poor;
            }
            
            if (info.ConsoleHost.Contains("cmd", StringComparison.OrdinalIgnoreCase))
            {
                return ArabicSupportLevel.Fair;
            }
            
            return ArabicSupportLevel.Unknown;
        }

        /// <summary>
        /// Attempts to launch ArbSh in Windows Terminal.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>True if successfully launched</returns>
        private static bool TryLaunchInWindowsTerminal(string[] args)
        {
            try
            {
                // Check if Windows Terminal is available
                var wtPath = FindWindowsTerminal();
                if (string.IsNullOrEmpty(wtPath))
                {
                    return false;
                }

                // Get current executable path
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExe))
                {
                    return false;
                }

                // Build command line
                var argsString = string.Join(" ", args);
                var command = $"\"{currentExe}\" {argsString}";

                // Launch in Windows Terminal
                var startInfo = new ProcessStartInfo
                {
                    FileName = wtPath,
                    Arguments = $"-- {command}",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG: Failed to launch in Windows Terminal: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to launch ArbSh in Command Prompt.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>True if successfully launched</returns>
        private static bool TryLaunchInCommandPrompt(string[] args)
        {
            try
            {
                // Get current executable path
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExe))
                {
                    return false;
                }

                // Build command line
                var argsString = string.Join(" ", args);
                var command = $"\"{currentExe}\" {argsString}";

                // Launch in Command Prompt
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG: Failed to launch in Command Prompt: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Finds the Windows Terminal executable path.
        /// </summary>
        /// <returns>Path to Windows Terminal or null if not found</returns>
        private static string? FindWindowsTerminal()
        {
            try
            {
                // Try common Windows Terminal locations
                var possiblePaths = new[]
                {
                    @"C:\Users\{0}\AppData\Local\Microsoft\WindowsApps\wt.exe",
                    @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_*\wt.exe"
                };

                var username = Environment.UserName;
                
                foreach (var pathTemplate in possiblePaths)
                {
                    var path = string.Format(pathTemplate, username);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
        
        #endregion

        #region Supporting Types
        
        /// <summary>
        /// Information about the current console environment.
        /// </summary>
        public class ConsoleInfo
        {
            public string ConsoleHost { get; set; } = "Unknown";
            public bool IsWindowsTerminal { get; set; }
            public bool IsPowerShell { get; set; }
            public ArabicSupportLevel ArabicSupport { get; set; }
            public bool IsInputRedirected { get; set; }
            public bool IsOutputRedirected { get; set; }
        }

        /// <summary>
        /// Levels of Arabic text support in different console environments.
        /// </summary>
        public enum ArabicSupportLevel
        {
            Unknown,
            Poor,      // PowerShell - poor Arabic support
            Fair,      // Command Prompt - basic Arabic support
            Good       // Windows Terminal - good Arabic support
        }
        
        #endregion
    }
}
