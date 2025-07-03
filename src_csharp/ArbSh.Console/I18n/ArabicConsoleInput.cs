using System;
using System.IO;
using System.Text;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Enhanced console input handler specifically designed for Arabic text support.
    /// Provides multiple input strategies with fallback mechanisms for Windows console limitations.
    /// </summary>
    public static class ArabicConsoleInput
    {
        #region Input Strategy Enumeration
        
        /// <summary>
        /// Available input strategies for handling Arabic text.
        /// </summary>
        public enum InputStrategy
        {
            /// <summary>Auto-detect the best available strategy</summary>
            Auto,
            /// <summary>Use Windows Console API (ReadConsoleW)</summary>
            WindowsConsoleApi,
            /// <summary>Use StreamReader with UTF-8 encoding</summary>
            StreamReader,
            /// <summary>Use standard Console.ReadLine</summary>
            StandardConsole
        }
        
        #endregion

        #region Private Fields
        
        private static InputStrategy _currentStrategy = InputStrategy.Auto;
        private static StreamReader? _stdinReader = null;
        private static bool _isInitialized = false;
        
        #endregion

        #region Public Interface
        
        /// <summary>
        /// Initializes the Arabic console input system with the specified strategy.
        /// </summary>
        /// <param name="strategy">The input strategy to use</param>
        public static void Initialize(InputStrategy strategy = InputStrategy.Auto)
        {
            if (_isInitialized)
            {
                return; // Already initialized
            }

            _currentStrategy = strategy;
            
            // Auto-detect the best strategy if requested
            if (_currentStrategy == InputStrategy.Auto)
            {
                _currentStrategy = DetectBestStrategy();
            }

            // Initialize StreamReader if needed
            if (_currentStrategy == InputStrategy.StreamReader)
            {
                InitializeStreamReader();
            }

            _isInitialized = true;
            
            System.Console.WriteLine($"DEBUG: ArabicConsoleInput initialized with strategy: {_currentStrategy}");
        }

        /// <summary>
        /// Reads a line of input using the configured Arabic-aware strategy.
        /// </summary>
        /// <returns>The input line, or null if EOF occurred</returns>
        public static string? ReadLine()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            string? result = null;
            
            try
            {
                switch (_currentStrategy)
                {
                    case InputStrategy.WindowsConsoleApi:
                        result = ReadLineWithWindowsApi();
                        break;
                        
                    case InputStrategy.StreamReader:
                        result = ReadLineWithStreamReader();
                        break;
                        
                    case InputStrategy.StandardConsole:
                        result = System.Console.ReadLine();
                        break;
                        
                    default:
                        result = System.Console.ReadLine();
                        break;
                }

                // Validate and sanitize the input
                result = ValidateAndSanitizeInput(result);
                
                return result;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG: ArabicConsoleInput.ReadLine failed: {ex.Message}");
                
                // Fallback to standard console input
                try
                {
                    return System.Console.ReadLine();
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets detailed information about the current input configuration.
        /// </summary>
        /// <returns>Configuration information for debugging</returns>
        public static string GetInputInfo()
        {
            var info = new StringBuilder();
            info.AppendLine("=== Arabic Console Input Information ===");
            info.AppendLine($"Initialized: {_isInitialized}");
            info.AppendLine($"Current Strategy: {_currentStrategy}");
            info.AppendLine($"Input Redirected: {System.Console.IsInputRedirected}");
            info.AppendLine($"Console Input Encoding: {System.Console.InputEncoding.EncodingName}");
            info.AppendLine($"Console Output Encoding: {System.Console.OutputEncoding.EncodingName}");
            
            if (WindowsConsoleApi.IsSupported())
            {
                info.AppendLine();
                info.AppendLine(WindowsConsoleApi.GetConsoleInfo());
            }
            
            return info.ToString();
        }

        /// <summary>
        /// Cleans up resources used by the Arabic console input system.
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                _stdinReader?.Dispose();
                _stdinReader = null;
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG: ArabicConsoleInput.Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods
        
        /// <summary>
        /// Detects the best input strategy for the current environment.
        /// </summary>
        /// <returns>The recommended input strategy</returns>
        private static InputStrategy DetectBestStrategy()
        {
            // If input is redirected, use StreamReader
            if (System.Console.IsInputRedirected)
            {
                return InputStrategy.StreamReader;
            }

            // If Windows Console API is available, prefer it for Arabic input
            if (WindowsConsoleApi.IsSupported())
            {
                return InputStrategy.WindowsConsoleApi;
            }

            // Fallback to StreamReader for better UTF-8 handling
            return InputStrategy.StreamReader;
        }

        /// <summary>
        /// Reads input using Windows Console API.
        /// </summary>
        /// <returns>Input line or null if failed</returns>
        private static string? ReadLineWithWindowsApi()
        {
            return WindowsConsoleApi.ReadLine();
        }

        /// <summary>
        /// Reads input using StreamReader with UTF-8 encoding.
        /// </summary>
        /// <returns>Input line or null if failed</returns>
        private static string? ReadLineWithStreamReader()
        {
            if (_stdinReader == null)
            {
                InitializeStreamReader();
            }

            return _stdinReader?.ReadLine();
        }

        /// <summary>
        /// Initializes the StreamReader for UTF-8 input.
        /// </summary>
        private static void InitializeStreamReader()
        {
            try
            {
                _stdinReader?.Dispose(); // Clean up existing reader
                _stdinReader = new StreamReader(System.Console.OpenStandardInput(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG: Failed to initialize StreamReader: {ex.Message}");
                _stdinReader = null;
            }
        }

        /// <summary>
        /// Validates and sanitizes input to handle Windows console encoding issues.
        /// </summary>
        /// <param name="input">Raw input string</param>
        /// <returns>Validated and sanitized input</returns>
        private static string? ValidateAndSanitizeInput(string? input)
        {
            if (input == null)
            {
                return null;
            }

            // Check for null character conversion issues (Arabic → U+0000)
            if (input.Contains('\0'))
            {
                System.Console.WriteLine("DEBUG: Detected null characters in input - possible Arabic encoding issue");
                
                // Count null characters vs total length for diagnostics
                int nullCount = 0;
                foreach (char c in input)
                {
                    if (c == '\0') nullCount++;
                }
                
                System.Console.WriteLine($"DEBUG: Input length: {input.Length}, Null chars: {nullCount}");
                
                // If the input is mostly null characters, it's likely Arabic that got converted
                if (nullCount > 0)
                {
                    System.Console.WriteLine("WARNING: Arabic characters may have been converted to null characters");
                    System.Console.WriteLine("This is a known Windows Console limitation with RTL text input");
                    
                    // For now, return the input as-is but log the issue
                    // In the future, we might implement character recovery strategies
                }
            }

            return input;
        }
        
        #endregion
    }
}
