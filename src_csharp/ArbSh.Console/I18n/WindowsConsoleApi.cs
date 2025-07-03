using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Direct Windows Console API wrapper to handle Arabic input properly.
    /// Uses ReadConsoleW to bypass Console.ReadLine limitations with RTL text.
    /// </summary>
    public static class WindowsConsoleApi
    {
        #region Windows API Constants
        
        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        
        // Console input modes
        private const uint ENABLE_LINE_INPUT = 0x0002;
        private const uint ENABLE_ECHO_INPUT = 0x0004;
        private const uint ENABLE_PROCESSED_INPUT = 0x0001;
        private const uint ENABLE_WINDOW_INPUT = 0x0008;
        private const uint ENABLE_MOUSE_INPUT = 0x0010;
        private const uint ENABLE_INSERT_MODE = 0x0020;
        private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        private const uint ENABLE_AUTO_POSITION = 0x0100;
        private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
        
        #endregion

        #region Windows API Imports
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ReadConsoleW(
            IntPtr hConsoleInput,
            [Out] StringBuilder lpBuffer,
            uint nNumberOfCharsToRead,
            out uint lpNumberOfCharsRead,
            IntPtr lpInputControl);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetLastError();
        
        #endregion

        #region Public Interface
        
        /// <summary>
        /// Reads a line of input from the console using Windows Console API.
        /// This bypasses Console.ReadLine to properly handle Arabic characters.
        /// </summary>
        /// <returns>The input line, or null if EOF or error occurred</returns>
        public static string? ReadLine()
        {
            try
            {
                IntPtr inputHandle = GetStdHandle(STD_INPUT_HANDLE);
                if (inputHandle == IntPtr.Zero || inputHandle == new IntPtr(-1))
                {
                    System.Console.WriteLine("DEBUG: Failed to get stdin handle, falling back to Console.ReadLine");
                    return System.Console.ReadLine();
                }

                // Configure console mode for optimal Arabic input
                if (!ConfigureConsoleMode(inputHandle))
                {
                    System.Console.WriteLine("DEBUG: Failed to configure console mode, falling back to Console.ReadLine");
                    return System.Console.ReadLine();
                }

                // Read input using ReadConsoleW
                const uint bufferSize = 1024;
                StringBuilder buffer = new StringBuilder((int)bufferSize);
                
                bool success = ReadConsoleW(
                    inputHandle,
                    buffer,
                    bufferSize,
                    out uint charsRead,
                    IntPtr.Zero);

                if (!success)
                {
                    uint error = GetLastError();
                    System.Console.WriteLine($"DEBUG: ReadConsoleW failed with error {error}, falling back to Console.ReadLine");
                    return System.Console.ReadLine();
                }

                if (charsRead == 0)
                {
                    return null; // EOF
                }

                // Convert to string and remove trailing newline characters
                string result = buffer.ToString(0, (int)charsRead);
                result = result.TrimEnd('\r', '\n');
                
                return result;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG: WindowsConsoleApi.ReadLine exception: {ex.Message}");
                return System.Console.ReadLine(); // Fallback to standard method
            }
        }

        /// <summary>
        /// Checks if the current environment supports Windows Console API.
        /// </summary>
        /// <returns>True if Windows Console API is available</returns>
        public static bool IsSupported()
        {
            try
            {
                // Check if we're on Windows
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return false;
                }

                // Try to get the console handle
                IntPtr inputHandle = GetStdHandle(STD_INPUT_HANDLE);
                return inputHandle != IntPtr.Zero && inputHandle != new IntPtr(-1);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets detailed information about the current console environment.
        /// </summary>
        /// <returns>Console environment information for debugging</returns>
        public static string GetConsoleInfo()
        {
            var info = new StringBuilder();
            info.AppendLine("=== Windows Console API Information ===");
            
            try
            {
                info.AppendLine($"OS Platform: {RuntimeInformation.OSDescription}");
                info.AppendLine($"Is Windows: {RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}");
                info.AppendLine($"API Supported: {IsSupported()}");
                
                if (IsSupported())
                {
                    IntPtr inputHandle = GetStdHandle(STD_INPUT_HANDLE);
                    info.AppendLine($"Input Handle: 0x{inputHandle.ToInt64():X}");
                    
                    if (GetConsoleMode(inputHandle, out uint mode))
                    {
                        info.AppendLine($"Console Mode: 0x{mode:X}");
                        info.AppendLine($"  - Line Input: {(mode & ENABLE_LINE_INPUT) != 0}");
                        info.AppendLine($"  - Echo Input: {(mode & ENABLE_ECHO_INPUT) != 0}");
                        info.AppendLine($"  - Processed Input: {(mode & ENABLE_PROCESSED_INPUT) != 0}");
                        info.AppendLine($"  - Virtual Terminal: {(mode & ENABLE_VIRTUAL_TERMINAL_INPUT) != 0}");
                    }
                }
            }
            catch (Exception ex)
            {
                info.AppendLine($"Error getting console info: {ex.Message}");
            }
            
            return info.ToString();
        }

        #endregion

        #region Private Methods
        
        /// <summary>
        /// Configures the console input mode for optimal Arabic text handling.
        /// </summary>
        /// <param name="inputHandle">Console input handle</param>
        /// <returns>True if configuration was successful</returns>
        private static bool ConfigureConsoleMode(IntPtr inputHandle)
        {
            try
            {
                // Get current console mode
                if (!GetConsoleMode(inputHandle, out uint currentMode))
                {
                    return false;
                }

                // Configure mode for line input with echo and processing
                // This should help with Arabic character input
                uint newMode = ENABLE_LINE_INPUT | 
                              ENABLE_ECHO_INPUT | 
                              ENABLE_PROCESSED_INPUT |
                              ENABLE_INSERT_MODE;

                // Try to enable virtual terminal input if available (Windows 10+)
                newMode |= ENABLE_VIRTUAL_TERMINAL_INPUT;

                return SetConsoleMode(inputHandle, newMode);
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
    }
}
