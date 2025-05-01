using System;
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO; // For StreamWriter
using ArbSh.Console.Commands;

namespace ArbSh.Console
{
    /// <summary>
    /// Responsible for executing parsed commands and managing the pipeline.
    /// (Placeholder implementation)
    /// </summary>
    public static class Executor
    {
        /// <summary>
        /// Executes a list of parsed commands.
        /// (Placeholder - needs actual execution logic)
        /// </summary>
        /// <param name="parsedCommands">A list of ParsedCommand objects representing the commands to execute.</param>
        public static void Execute(List<ParsedCommand> parsedCommands)
        {
            // TODO: Implement actual execution logic.
            // This should handle:
            // - Iterating through commands (if separated by ';')
            // - Setting up pipeline stages (if separated by '|')
            // - Finding and instantiating the correct CmdletBase derived classes based on command name.
            // - Binding parameters to cmdlet properties.
            // - Calling BeginProcessing, ProcessRecord (feeding pipeline objects), EndProcessing on cmdlets.
            // - Managing the flow of PipelineObjects between cmdlets.
            // - Handling output, errors, warnings.
            // - Executing external processes if the command is not a cmdlet.

            System.Console.WriteLine($"DEBUG (Executor): Executing {parsedCommands.Count} command(s)...");

            // TODO: This currently executes commands sequentially.
            // Real pipeline execution requires concurrent execution and data streaming.
            BlockingCollection<PipelineObject>? currentInput = null; // Input for the *first* command is null initially

            for (int i = 0; i < parsedCommands.Count; i++)
            {
                var command = parsedCommands[i];
                bool isLastCommand = (i == parsedCommands.Count - 1);

                string commandName = command.CommandName;
                List<string> arguments = command.Arguments; // Use arguments from ParsedCommand

                System.Console.WriteLine($"DEBUG (Executor Pipeline): Processing stage {i}: '{commandName}'...");

                // --- Cmdlet Discovery and Instantiation ---
                CmdletBase? cmdlet = null; // Renamed from cmdletInstance
                Type? cmdletType = CommandDiscovery.Find(commandName);

                if (cmdletType != null)
                {
                    try
                    {
                        // Create an instance of the found cmdlet type
                        cmdlet = Activator.CreateInstance(cmdletType) as CmdletBase;
                        if (cmdlet == null)
                        {
                             System.Console.WriteLine($"ERROR: Failed to activate cmdlet type {cmdletType.FullName}");
                        }
                    }
                    catch (Exception ex)
                    {
                         System.Console.WriteLine($"ERROR: Failed to create instance of {cmdletType.FullName}: {ex.Message}");
                         cmdlet = null; // Ensure cmdlet is null if activation failed
                    }
                }
                // TODO: Add logic here to check for external commands if cmdletType is null

                if (cmdlet != null)
                    {
                        bool bindingSuccessful = true;
                        try
                        {
                            // --- Parameter Binding Step ---
                            BindParameters(cmdlet, command);
                            // -----------------------------
                        }
                        catch (ParameterBindingException bindEx)
                        {
                            System.Console.ForegroundColor = ConsoleColor.Red;
                            System.Console.WriteLine($"ParameterBindingError: {bindEx.Message}");
                            System.Console.ResetColor();
                            bindingSuccessful = false;
                            // Stop processing this pipeline stage
                        }
                        catch (Exception ex) // Catch other potential reflection errors during binding
                        {
                             System.Console.ForegroundColor = ConsoleColor.Red;
                             System.Console.WriteLine($"Unexpected Binding Error: {ex.Message}");
                             System.Console.ResetColor();
                             bindingSuccessful = false;
                        }

                        if (bindingSuccessful)
                        {
                            // Prepare output collection for this cmdlet
                        var outputCollection = new BlockingCollection<PipelineObject>();
                        cmdlet.OutputCollection = outputCollection; // Assign it to the cmdlet instance

                        try
                        {
                            cmdlet.BeginProcessing();

                            // Process pipeline input (if any)
                            if (currentInput != null)
                            {
                                // Consume the input from the previous command
                                foreach (var inputObject in currentInput.GetConsumingEnumerable())
                                {
                                    cmdlet.ProcessRecord(inputObject);
                                }
                            }
                            else
                            {
                                // Handle cmdlets that generate output without pipeline input
                                // e.g., simulate argument processing for Write-Output if no pipeline input
                                if (cmdlet is WriteOutputCmdlet && command.Arguments.Count > 0)
                                {
                                    string outputValue = string.Join(" ", command.Arguments);
                                    cmdlet.ProcessRecord(new PipelineObject(outputValue));
                                }
                            }

                            cmdlet.EndProcessing();
                        }
                        catch (Exception ex)
                        {
                            // Handle cmdlet-specific errors
                            System.Console.ForegroundColor = ConsoleColor.Magenta;
                            System.Console.WriteLine($"Cmdlet Error ({commandName}): {ex.Message}");
                            System.Console.ResetColor();
                            // Stop pipeline execution on error? Or allow downstream? TBD.
                            break; // Stop processing further commands for now
                        }
                        finally
                        {
                            // Signal that this cmdlet is done adding to its output
                            outputCollection.CompleteAdding();
                        }

                            // Set the output of this command as the input for the next
                            currentInput = outputCollection;
                        }
                        else
                        {
                            // If binding failed, stop processing this pipeline
                             System.Console.WriteLine($"DEBUG (Executor): Halting pipeline due to parameter binding error.");
                             break;
                        }

                    }
                    else // Cmdlet not found
                    {
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        System.Console.WriteLine($"Command not found: {commandName}");
                        System.Console.ResetColor();
                        break; // Stop processing further commands if one is not found
                    }
            } // End of pipeline loop

            // --- Output Handling ---
            // Handle output from the last command in the pipeline
            if (currentInput != null)
            {
                ParsedCommand lastCommand = parsedCommands.Last(); // Get the last command for redirection info
                StreamWriter? redirectWriter = null;

                try
                {
                    // Check for output redirection on the last command
                    if (!string.IsNullOrEmpty(lastCommand.OutputRedirectPath))
                    {
                        try
                        {
                            FileMode mode = lastCommand.AppendOutput ? FileMode.Append : FileMode.Create;
                            // Using UTF8 encoding by default
                            redirectWriter = new StreamWriter(lastCommand.OutputRedirectPath, lastCommand.AppendOutput, System.Text.Encoding.UTF8);
                            System.Console.WriteLine($"DEBUG (Executor): Redirecting output {(lastCommand.AppendOutput ? ">>" : ">")} {lastCommand.OutputRedirectPath}");
                        }
                        catch (Exception ex)
                        {
                             System.Console.ForegroundColor = ConsoleColor.Red;
                             System.Console.WriteLine($"ERROR: Cannot open file '{lastCommand.OutputRedirectPath}' for redirection: {ex.Message}");
                             System.Console.ResetColor();
                             // Don't proceed with output if redirection failed
                             currentInput.Dispose(); // Dispose the collection
                             currentInput = null;
                        }
                    }

                    if (currentInput != null) // Check again in case redirection failed
                    {
                        if (redirectWriter == null) {
                             System.Console.WriteLine("DEBUG (Executor Pipeline): Final pipeline output to Console:");
                        }

                        foreach (var finalOutput in currentInput.GetConsumingEnumerable()) // Consume final output
                        {
                            // TODO: Implement proper formatting based on object type
                            string outputString = finalOutput.ToString() ?? string.Empty;

                            if (redirectWriter != null)
                            {
                                redirectWriter.WriteLine(outputString);
                            }
                            else
                            {
                                System.Console.WriteLine(outputString);
                            }
                     }
                 }
                 }
                 catch (OperationCanceledException) { /* Expected if collection is empty and completed */ }
                 finally
                 {
                     redirectWriter?.Dispose(); // Ensure file stream is closed
                 }
            }
             System.Console.WriteLine($"DEBUG (Executor): Pipeline execution finished.");
        }

        /// <summary>
        /// Binds parameters from the parsed command to the cmdlet instance using reflection.
        /// (Basic placeholder implementation)
        /// </summary>
        private static void BindParameters(CmdletBase cmdlet, ParsedCommand command)
        {
             System.Console.WriteLine($"DEBUG (Binder): Binding parameters for {cmdlet.GetType().Name}...");
             var cmdletType = cmdlet.GetType();
             var properties = cmdletType.GetProperties()
                 .Where(p => Attribute.IsDefined(p, typeof(ParameterAttribute)))
                 .ToList();

             // Keep track of used positional arguments
             var usedPositionalArgs = new bool[command.Arguments.Count];

             foreach (var prop in properties)
             {
                 var paramAttr = prop.GetCustomAttribute<ParameterAttribute>();
                 if (paramAttr == null) continue;

                 string paramName = $"-{prop.Name}"; // Convention: -PropertyName
                 object? valueToSet = null;
                 bool found = false;

                 // 1. Try binding by parameter name
                 if (command.Parameters.ContainsKey(paramName))
                 {
                     string? namedValue = command.Parameters[paramName]; // May be empty for switch parameters

                     // Handle boolean switch parameters
                     if (prop.PropertyType == typeof(bool))
                     {
                         // If parameter name is present, it implies true unless a value $false is explicitly given
                         if (string.IsNullOrEmpty(namedValue)) // e.g., -Full
                         {
                             valueToSet = true;
                             found = true;
                             System.Console.WriteLine($"DEBUG (Binder): Bound switch parameter '{paramName}' to true (no value provided).");
                         }
                         else // e.g., -Full $true, -Full $false, -Full someValue
                         {
                             if (bool.TryParse(namedValue, out bool boolValue))
                             {
                                 valueToSet = boolValue;
                                 found = true;
                                 System.Console.WriteLine($"DEBUG (Binder): Bound boolean parameter '{paramName}' to explicit value '{valueToSet}'.");
                             }
                             else
                             {
                                 // Value provided for bool param, but it's not 'true' or 'false' - this is an error.
                                 throw new ParameterBindingException($"A value '{namedValue}' cannot be specified for the boolean (switch) parameter '{paramName}'.");
                             }
                         }
                     }
                     // Attempt type conversion for parameters with non-empty values
                     else if (!string.IsNullOrEmpty(namedValue))
                     {
                         try
                         {
                             // Attempt conversion using TypeConverter first, then fallback
                             TypeConverter converter = TypeDescriptor.GetConverter(prop.PropertyType);
                             if (converter != null && converter.CanConvertFrom(typeof(string)))
                             {
                                 valueToSet = converter.ConvertFromString(namedValue);
                             }
                             else
                             {
                                 // Fallback for basic types if no specific converter found/applicable
                                 valueToSet = Convert.ChangeType(namedValue, prop.PropertyType);
                             }
                             found = true;
                             System.Console.WriteLine($"DEBUG (Binder): Bound named parameter '{paramName}' to value '{valueToSet}' (Type: {prop.PropertyType.Name})");
                         }
                         catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                         {
                              System.Console.WriteLine($"WARN (Binder): Type conversion failed for named parameter '{paramName}' value '{namedValue}' to {prop.PropertyType.Name}. Error: {ex.Message}");
                         }
                     }
                 }

                 // 2. Try binding by position (if not found by name and attribute specifies position)
                 if (!found && paramAttr.Position >= 0 && paramAttr.Position < command.Arguments.Count && !usedPositionalArgs[paramAttr.Position])
                 {
                     string positionalValue = command.Arguments[paramAttr.Position];
                     try
                     {
                         // Attempt conversion using TypeConverter first, then fallback
                         TypeConverter converter = TypeDescriptor.GetConverter(prop.PropertyType);
                         if (converter != null && converter.CanConvertFrom(typeof(string)))
                         {
                             valueToSet = converter.ConvertFromString(positionalValue);
                         }
                         else
                         {
                             // Fallback for basic types
                             valueToSet = Convert.ChangeType(positionalValue, prop.PropertyType);
                         }
                         found = true;
                         usedPositionalArgs[paramAttr.Position] = true; // Mark as used
                         System.Console.WriteLine($"DEBUG (Binder): Bound positional parameter at {paramAttr.Position} ('{positionalValue}') to property '{prop.Name}' (Type: {prop.PropertyType.Name})");
                     }
                     catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                     {
                          System.Console.WriteLine($"WARN (Binder): Type conversion failed for positional parameter at {paramAttr.Position} value '{positionalValue}' to {prop.PropertyType.Name}. Error: {ex.Message}");
                     }
                 }

                 // 3. Set the property value if found
                 if (found && valueToSet != null)
                 {
                     try
                     {
                         prop.SetValue(cmdlet, valueToSet);
                     }
                     catch (Exception ex)
                     {
                         System.Console.WriteLine($"ERROR (Binder): Failed to set property '{prop.Name}': {ex.Message}");
                     }
                 }

                 // 4. Check for Mandatory parameters that were not bound
                 if (!found && paramAttr.Mandatory)
                 {
                      // Throw an exception to stop execution
                      throw new ParameterBindingException($"Missing mandatory parameter '{paramName}' for cmdlet '{cmdlet.GetType().Name}'.")
                      {
                          ParameterName = prop.Name // Store the property name
                      };
                 }
             }

             // TODO: Handle remaining positional arguments (e.g., pass via pipeline or error)
             for(int i = 0; i < command.Arguments.Count; i++)
             {
                 if (!usedPositionalArgs[i])
                 {
                      System.Console.WriteLine($"WARN (Binder): Unused positional argument: {command.Arguments[i]}");
                 }
             }
        }
    }
}
