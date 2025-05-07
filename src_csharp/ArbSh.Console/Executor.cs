using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO; // For StreamWriter
using System.Threading.Tasks; // Added for Task support
using ArbSh.Console.Commands;
using static ArbSh.Console.ParsedCommand; // For RedirectionInfo etc.

namespace ArbSh.Console
{
    /// <summary>
    /// Responsible for executing parsed commands and managing the pipeline using Tasks for concurrency.
    /// </summary>
    public static class Executor
    {
        /// <summary>
        /// Executes a list of statements, where each statement is a list of parsed commands forming a pipeline.
        /// Executes pipeline stages concurrently within each statement.
        /// </summary>
        /// <param name="allStatements">A list where each element is a list of ParsedCommand objects for a single statement's pipeline.</param>
        public static void Execute(List<List<ParsedCommand>> allStatements)
        {
            System.Console.WriteLine($"DEBUG (Executor): Executing {allStatements.Count} statement(s)...");

            foreach (var statementCommands in allStatements)
            {
                if (statementCommands == null || statementCommands.Count == 0)
                {
                    continue; // Skip empty statements
                }

                System.Console.WriteLine($"DEBUG (Executor): --- Executing Statement ({statementCommands.Count} command(s)) ---");

                // Pipeline execution using Tasks for concurrency.
                BlockingCollection<PipelineObject>? inputForCurrentStage = null; 
                List<Task> pipelineTasks = new List<Task>(); // List to hold tasks for the current pipeline
                BlockingCollection<PipelineObject>? outputOfLastStage = null; // To hold the final output collection
                StreamReader? inputRedirectReader = null; // For handling < redirection

                // --- Handle Input Redirection for the FIRST command ---
                if (statementCommands.Count > 0 && !string.IsNullOrEmpty(statementCommands[0].InputRedirectPath))
                {
                    string inputFile = statementCommands[0].InputRedirectPath!;
                    System.Console.WriteLine($"DEBUG (Executor): Attempting input redirection from '{inputFile}' for first command.");
                    try
                    {
                        // Use UTF8 without BOM by default for reading
                        var utf8NoBom = new System.Text.UTF8Encoding(false); 
                        inputRedirectReader = new StreamReader(inputFile, utf8NoBom);
                        
                        // Prepare a collection to feed the file content into the first stage
                        var fileInputCollection = new BlockingCollection<PipelineObject>();
                        inputForCurrentStage = fileInputCollection; // This will be the input for the first stage

                        // Start a task to read the file and populate the collection asynchronously
                        // This prevents blocking the main executor thread if the file is large.
                        Task.Run(() => {
                            try
                            {
                                string? line;
                                while ((line = inputRedirectReader.ReadLine()) != null)
                                {
                                    fileInputCollection.Add(new PipelineObject(line));
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log error reading file - maybe add error object to collection?
                                System.Console.ForegroundColor = ConsoleColor.Red;
                                System.Console.WriteLine($"ERROR reading input redirect file '{inputFile}': {ex.Message}");
                                System.Console.ResetColor();
                                // Add an error object to signal downstream cmdlets?
                                fileInputCollection.Add(new PipelineObject($"ERROR reading input file: {ex.Message}", isError: true));
                            }
                            finally
                            {
                                fileInputCollection.CompleteAdding(); // Signal end of file input
                                inputRedirectReader?.Dispose(); // Dispose the reader when done
                                System.Console.WriteLine($"DEBUG (Executor): Finished reading input redirect file '{inputFile}'. Input collection marked complete.");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        // Handle file open errors (e.g., FileNotFoundException, IOException)
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine($"ERROR opening input redirect file '{inputFile}': {ex.Message}");
                        System.Console.ResetColor();
                        // Can't proceed with this statement if input redirection fails critically
                        // We could add an error object to a dummy input collection, or just skip the statement.
                        // Let's skip the statement for now.
                        inputForCurrentStage = new BlockingCollection<PipelineObject>(); // Provide empty input
                        inputForCurrentStage.CompleteAdding(); // Mark as complete immediately
                        // Skip setting up tasks for this statement by breaking the loop
                        break; 
                    }
                }
                // If no input redirection, inputForCurrentStage remains null for the first command.


                for (int i = 0; i < statementCommands.Count; i++)
                {
                    // Capture loop variables for closure to avoid issues in lambda expressions
                    var currentCommand = statementCommands[i];
                    var currentInputCollection = inputForCurrentStage; // Input for *this* stage comes from the previous stage's output
                    var outputCollection = new BlockingCollection<PipelineObject>(); // Output collection for *this* stage
                    bool isLastStage = (i == statementCommands.Count - 1);

                    // Prepare the output of this stage to be the input for the next stage
                    inputForCurrentStage = outputCollection;
                    if (isLastStage)
                    {
                        outputOfLastStage = outputCollection; // Keep track of the final stage's output collection
                    }

                    string commandName = currentCommand.CommandName; // Use captured variable
                    System.Console.WriteLine($"DEBUG (Executor Pipeline): Preparing stage {i}: '{commandName}'...");

                    // --- Cmdlet Discovery ---
                    Type? cmdletType = CommandDiscovery.Find(commandName);

                    // TODO: Add logic here to check for external commands if cmdletType is null
                    // TODO: Implement redirection handling *before* cmdlet execution (e.g., setting StdOut/StdErr for the task/process)

                    if (cmdletType != null)
                    {
                        // --- Create and add the task for this pipeline stage ---
                        var pipelineTask = Task.Run(() =>
                        {
                            CmdletBase? cmdletInstance = null; // Instance specific to this task
                            System.Console.WriteLine($"DEBUG (Executor Task): Starting task for '{currentCommand.CommandName}'...");
                            try
                            {
                                // --- Instantiate Cmdlet (Inside Task) ---
                                cmdletInstance = Activator.CreateInstance(cmdletType) as CmdletBase;
                                if (cmdletInstance == null)
                                {
                                    // Log error and potentially signal pipeline failure
                                    throw new InvalidOperationException($"Failed to activate cmdlet type {cmdletType.FullName}");
                                }

                                // --- Parameter Binding Step (Inside Task) ---
                                BindParameters(cmdletInstance, currentCommand); // Use captured command

                                // Assign the output collection for this stage to the cmdlet instance
                                cmdletInstance.OutputCollection = outputCollection;

                                // --- Cmdlet Execution Lifecycle (Inside Task) ---
                                cmdletInstance.BeginProcessing();

                                // Process pipeline input (if any) from the previous stage
                                if (currentInputCollection != null)
                                {
                                    System.Console.WriteLine($"DEBUG (Executor Task): '{currentCommand.CommandName}' consuming input...");
                                    // Consume the input from the previous command's output collection
                                    // GetConsumingEnumerable blocks until the collection is marked CompleteAdding or items are available
                                    foreach (var inputObject in currentInputCollection.GetConsumingEnumerable())
                                    {
                                        // Bind parameters that accept pipeline input *before* calling ProcessRecord
                                        cmdletInstance.BindPipelineParameters(inputObject);

                                        // Now process the record
                                        cmdletInstance.ProcessRecord(inputObject);
                                    }
                                    System.Console.WriteLine($"DEBUG (Executor Task): '{currentCommand.CommandName}' finished consuming input.");
                                }
                                else
                                {
                                    System.Console.WriteLine($"DEBUG (Executor Task): '{currentCommand.CommandName}' has no pipeline input, calling ProcessRecord once.");
                                    // Call ProcessRecord once even without pipeline input,
                                    // allowing cmdlets like Get-Command or Write-Output with arguments to run.
                                    // TODO: Handle subexpression arguments here - execute them first?
                                    cmdletInstance.ProcessRecord(null);
                                }

                                cmdletInstance.EndProcessing();
                                System.Console.WriteLine($"DEBUG (Executor Task): '{currentCommand.CommandName}' finished processing.");
                            }
                            catch (ParameterBindingException bindEx)
                            {
                                // Log binding errors - these stop the *current* cmdlet task
                                System.Console.ForegroundColor = ConsoleColor.Red;
                                System.Console.WriteLine($"Task Error (ParameterBinding) in '{currentCommand.CommandName}': {bindEx.Message}");
                                System.Console.ResetColor();
                                // Re-throw to mark the task as faulted
                                throw;
                            }
                            catch (Exception ex)
                            {
                                // Log general cmdlet execution errors
                                System.Console.ForegroundColor = ConsoleColor.Magenta;
                                System.Console.WriteLine($"Task Error in '{currentCommand.CommandName}': {ex.GetType().Name} - {ex.Message}");
                                // Consider adding ex.StackTrace for detailed debugging
                                System.Console.ResetColor();
                                // Re-throw to mark the task as faulted
                                throw;
                            }
                            finally
                            {
                                // CRITICAL: Signal that this stage is done adding items to its output collection.
                                // This unblocks the GetConsumingEnumerable() in the *next* stage's task (if any)
                                // or allows the final output handling to proceed.
                                outputCollection.CompleteAdding();
                                System.Console.WriteLine($"DEBUG (Executor Task): Stage '{currentCommand.CommandName}' completed adding output.");
                            }
                        }); // End Task.Run

                        pipelineTasks.Add(pipelineTask);
                    }
                    else // Cmdlet not found
                    {
                        // If a command isn't found, stop setting up the pipeline for this statement.
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        System.Console.WriteLine($"Command not found: {currentCommand.CommandName}. Halting pipeline setup for this statement.");
                        System.Console.ResetColor();

                        // Clean up collections created so far for this statement to prevent deadlocks
                        inputForCurrentStage?.CompleteAdding(); // Mark the current output as complete
                        currentInputCollection?.Dispose(); // Dispose the input collection we were going to read from
                        outputCollection.Dispose(); // Dispose the collection we just created

                        pipelineTasks.Clear(); // Prevent waiting on an incomplete/invalid pipeline
                        outputOfLastStage = null; // Ensure we don't try to process output from a failed pipeline
                        break; // Stop setting up more stages for this statement
                    }
                } // End of pipeline stage setup loop for the current statement

                // --- Wait for all tasks in the current statement's pipeline to complete ---
                if (pipelineTasks.Any())
                {
                    System.Console.WriteLine($"DEBUG (Executor): Waiting for {pipelineTasks.Count} task(s) in the pipeline to complete...");
                    try
                    {
                        // Wait for all tasks associated with the current statement's pipeline
                        Task.WhenAll(pipelineTasks).Wait();
                        System.Console.WriteLine($"DEBUG (Executor): All pipeline tasks for the statement completed.");
                    }
                    catch (AggregateException ae)
                    {
                        // Log errors from faulted tasks
                        System.Console.ForegroundColor = ConsoleColor.DarkRed;
                        System.Console.WriteLine($"ERROR (Executor): One or more pipeline tasks failed:");
                        foreach (var ex in ae.Flatten().InnerExceptions)
                        {
                            // Avoid logging the ParameterBindingException again if already logged in the task
                            if (!(ex is ParameterBindingException))
                            {
                                System.Console.WriteLine($"  - {ex.GetType().Name}: {ex.Message}");
                            }
                        }
                        System.Console.ResetColor();
                        // Pipeline failed, don't process output. Fall through to cleanup.
                        outputOfLastStage = null; // Prevent output processing
                    }
                    catch (Exception ex) // Catch other potential waiting errors
                    {
                        System.Console.ForegroundColor = ConsoleColor.DarkRed;
                        System.Console.WriteLine($"ERROR (Executor): Unexpected error waiting for pipeline tasks: {ex.Message}");
                        System.Console.ResetColor();
                        outputOfLastStage = null; // Prevent output processing
                    }
                }

                // --- Output Handling for the completed statement ---
                // Process the output from the *last* stage's collection, if the pipeline didn't fail
                if (outputOfLastStage != null)
                {
                    ParsedCommand lastCommandConfig = statementCommands.Last(); // Get config (like redirection) from the original last command
                    StreamWriter? stdoutRedirectWriter = null;
                    StreamWriter? stderrRedirectWriter = null; // Added for stderr
                    string? stdoutRedirectPath = null;
                    string? stderrRedirectPath = null;
                    bool writeStdOutToConsole = true;
                    bool writeStdErrToConsole = true;
                    bool mergeStderrToStdout = false; // Flag for 2>&1
                    bool mergeStdoutToStderr = false; // Flag for 1>&2

                    try
                    {
                        // --- Setup Redirections ---
                        if (lastCommandConfig.Redirections.Any())
                        {
                            // First pass: Detect stream merges
                            foreach (var redir in lastCommandConfig.Redirections.Where(r => r.TargetType == RedirectionTargetType.StreamHandle))
                            {
                                if (int.TryParse(redir.Target, out int targetHandle))
                                {
                                    if (redir.SourceStreamHandle == 2 && targetHandle == 1) // 2>&1
                                    {
                                        mergeStderrToStdout = true;
                                        System.Console.WriteLine($"DEBUG (Executor): Detected stderr merge to stdout (2>&1).");
                                    }
                                    else if (redir.SourceStreamHandle == 1 && targetHandle == 2) // 1>&2
                                    {
                                        mergeStdoutToStderr = true;
                                        System.Console.WriteLine($"DEBUG (Executor): Detected stdout merge to stderr (1>&2).");
                                    }
                                    else
                                    {
                                         System.Console.WriteLine($"WARN (Executor): Stream handle redirection from {redir.SourceStreamHandle} to {redir.Target} is parsed but not yet implemented.");
                                    }
                                }
                                else
                                {
                                     System.Console.WriteLine($"WARN (Executor): Invalid target stream handle '{redir.Target}' in redirection.");
                                }
                            }

                            // Second pass: Setup file writers and determine console output based on merges
                            // If stdout is redirected to a file OR merged to stderr, don't write stdout to console.
                            writeStdOutToConsole = !lastCommandConfig.Redirections.Any(r => r.SourceStreamHandle == 1 && r.TargetType == RedirectionTargetType.FilePath) && !mergeStdoutToStderr;
                            // If stderr is redirected to a file OR merged to stdout, don't write stderr to console.
                            writeStdErrToConsole = !lastCommandConfig.Redirections.Any(r => r.SourceStreamHandle == 2 && r.TargetType == RedirectionTargetType.FilePath) && !mergeStderrToStdout;


                            // Now setup file writers
                            foreach (var redir in lastCommandConfig.Redirections.Where(r => r.TargetType == RedirectionTargetType.FilePath))
                            {
                                // File redirections take precedence over merges for console output suppression
                                // (e.g., if 2>&1 and 1>file, stderr also goes to file, not console)
                                if (redir.TargetType == RedirectionTargetType.FilePath) // Double check, though filtered
                                {
                                    if (redir.SourceStreamHandle == 1) // Stdout to File
                                    {
                                        stdoutRedirectPath = redir.Target;
                                        try
                                        {
                                            var utf8NoBom = new System.Text.UTF8Encoding(false);
                                            stdoutRedirectWriter = new StreamWriter(stdoutRedirectPath, redir.Append, utf8NoBom);
                                            System.Console.WriteLine($"DEBUG (Executor): Redirecting stdout {(redir.Append ? ">>" : ">")} {stdoutRedirectPath}");
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Console.ForegroundColor = ConsoleColor.Red;
                                            System.Console.WriteLine($"ERROR: Cannot open file '{stdoutRedirectPath}' for stdout redirection: {ex.Message}");
                                            System.Console.ResetColor();
                                            stdoutRedirectWriter = null; // Ensure it's null
                                            writeStdOutToConsole = true; // Fallback to console if file open fails
                                        }
                                    }
                                    else if (redir.SourceStreamHandle == 2) // Stderr to File
                                    {
                                        stderrRedirectPath = redir.Target;
                                        try
                                        {
                                            var utf8NoBom = new System.Text.UTF8Encoding(false);
                                            stderrRedirectWriter = new StreamWriter(stderrRedirectPath, redir.Append, utf8NoBom);
                                            System.Console.WriteLine($"DEBUG (Executor): Redirecting stderr {(redir.Append ? "2>>" : "2>")} {stderrRedirectPath}");
                                            // writeStdErrToConsole = false; // Already handled above based on merge flags
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Console.ForegroundColor = ConsoleColor.Red;
                                            System.Console.WriteLine($"ERROR: Cannot open file '{stderrRedirectPath}' for stderr redirection: {ex.Message}");
                                            System.Console.ResetColor();
                                            stderrRedirectWriter = null;
                                            writeStdErrToConsole = true; // Fallback stderr to console
                                        }
                                    }
                                    else
                                    {
                                        System.Console.WriteLine($"WARN (Executor): File redirection from unsupported source handle '{redir.SourceStreamHandle}' ignored.");
                                    }
                                }
                                // Stream handle redirections were processed in the first pass
                            }
                        } // End if Redirections.Any()

                        // --- Consume and Distribute Output ---
                        System.Console.WriteLine($"DEBUG (Executor Output): Checking final output. IsCompleted={outputOfLastStage.IsCompleted}, Count={outputOfLastStage.Count}, IsAddingCompleted={outputOfLastStage.IsAddingCompleted}");
                        if (writeStdOutToConsole) System.Console.WriteLine("DEBUG (Executor Output): Writing final stdout output to Console...");
                        if (stdoutRedirectWriter != null) System.Console.WriteLine($"DEBUG (Executor Output): Writing final stdout output to file '{stdoutRedirectPath}'...");
                        if (writeStdErrToConsole) System.Console.WriteLine("DEBUG (Executor Output): Writing final stderr output to Console...");
                        if (stderrRedirectWriter != null) System.Console.WriteLine($"DEBUG (Executor Output): Writing final stderr output to file '{stderrRedirectPath}'...");

                        int outputCount = 0;
                        foreach (var finalOutput in outputOfLastStage.GetConsumingEnumerable())
                        {
                            outputCount++;
                            bool isError = finalOutput.IsError; // Use the flag from PipelineObject
                            string outputString = finalOutput?.ToString() ?? string.Empty;
                            System.Console.WriteLine($"DEBUG (Executor Output): Processing output item #{outputCount} (IsError={isError}): '{outputString}'");

                            // Determine target(s) based on error status and merge flags
                            // StreamWriter? primaryWriter = null; // Unused
                            // StreamWriter? secondaryWriter = null; // Unused
                            // string? primaryPath = null; // Unused
                            // bool writeToPrimaryConsole = false; // Unused
                            StreamWriter? targetFileWriter = null;
                            string? targetFilePath = null;
                            bool writeToConsole = false;
                            System.IO.TextWriter consoleStream = System.Console.Out; // Default to Stdout

                            if (isError) // This is an error object
                            {
                                if (mergeStderrToStdout) // 2>&1: Treat like normal output
                                {
                                    targetFileWriter = stdoutRedirectWriter;
                                    targetFilePath = stdoutRedirectPath;
                                    writeToConsole = writeStdOutToConsole; // Use stdout's console status
                                    consoleStream = System.Console.Out;    // Write to stdout console if applicable
                                    System.Console.WriteLine($"DEBUG (Executor Output): Routing error object via 2>&1 merge.");
                                }
                                else // Normal error (stderr) or 2>file
                                {
                                    targetFileWriter = stderrRedirectWriter;
                                    targetFilePath = stderrRedirectPath;
                                    writeToConsole = writeStdErrToConsole; // Use stderr's console status
                                    consoleStream = System.Console.Error;   // Write to stderr console if applicable
                                }
                            }
                            else // This is regular output object
                            {
                                if (mergeStdoutToStderr) // 1>&2: Treat like error output
                                {
                                    targetFileWriter = stderrRedirectWriter; // Target stderr's file writer
                                    targetFilePath = stderrRedirectPath;
                                    writeToConsole = writeStdErrToConsole; // Use stderr's console status
                                    consoleStream = System.Console.Error;   // Write to stderr console if applicable
                                    System.Console.WriteLine($"DEBUG (Executor Output): Routing regular object via 1>&2 merge.");
                                }
                                else // Normal output (stdout) or 1>file
                                {
                                    targetFileWriter = stdoutRedirectWriter;
                                    targetFilePath = stdoutRedirectPath;
                                    writeToConsole = writeStdOutToConsole; // Use stdout's console status
                                    consoleStream = System.Console.Out;    // Write to stdout console if applicable
                                }
                            }

                            // Write to File if redirected
                            if (targetFileWriter != null)
                            {
                                try
                                {
                                    targetFileWriter.WriteLine(outputString);
                                    targetFileWriter.Flush(); // Flush immediately for testing
                                    System.Console.WriteLine($"DEBUG (Executor Output): Item #{outputCount} written and flushed to target file '{targetFilePath}'.");
                                }
                                catch (Exception ex)
                                {
                                    System.Console.ForegroundColor = ConsoleColor.Red;
                                    System.Console.WriteLine($"ERROR: Failed writing/flushing to redirect file '{targetFilePath}': {ex.GetType().Name} - {ex.Message}");
                                    System.Console.ResetColor();
                                    try { targetFileWriter.Dispose(); } catch { /* Ignore */ }
                                    
                                    // Null out the writer that failed and potentially enable console fallback
                                    if (targetFileWriter == stdoutRedirectWriter) 
                                    {
                                        stdoutRedirectWriter = null;
                                        // If stdout file failed, enable console output for stdout *unless* it was merged to stderr
                                        if (!mergeStdoutToStderr) writeStdOutToConsole = true; 
                                    }
                                    if (targetFileWriter == stderrRedirectWriter) 
                                    {
                                         stderrRedirectWriter = null;
                                         // If stderr file failed, enable console output for stderr *unless* it was merged to stdout
                                         if (!mergeStderrToStdout) writeStdErrToConsole = true;
                                    }
                                    
                                    // Re-evaluate if we should write to console now based on updated flags
                                    if (isError) {
                                        writeToConsole = mergeStderrToStdout ? writeStdOutToConsole : writeStdErrToConsole;
                                    } else {
                                        writeToConsole = mergeStdoutToStderr ? writeStdErrToConsole : writeStdOutToConsole;
                                    }
                                    targetFileWriter = null; // Ensure we don't try to use it again
                                }
                            }
                            
                            // Write to Console if applicable (either originally intended or as fallback)
                            if (writeToConsole)
                            {
                                consoleStream.WriteLine(outputString);
                            }
                        }
                         if (outputCount == 0 && outputOfLastStage.IsAddingCompleted) {
                              System.Console.WriteLine("DEBUG (Executor Output): Final output collection is complete and empty.");
                         }
                    }
                    catch (OperationCanceledException) { /* Expected if collection is empty and completed */ }
                    catch (Exception ex)
                    {
                        // Catch unexpected errors during final output processing
                        System.Console.ForegroundColor = ConsoleColor.DarkCyan;
                        System.Console.WriteLine($"ERROR: Unexpected error processing final output: {ex.Message}");
                        System.Console.ResetColor();
                    }
                    finally
                    {
                        // Dispose any open stream writers
                        stdoutRedirectWriter?.Dispose(); // Use null-conditional operator
                        stderrRedirectWriter?.Dispose(); // Use null-conditional operator
                        
                        outputOfLastStage.Dispose(); // Dispose the final collection
                    }
                }
                else // outputOfLastStage was null (likely pipeline setup failed)
                {
                    // Check if there were any commands to begin with, to avoid redundant message if statement was empty
                    if (statementCommands.Any())
                    {
                        System.Console.WriteLine($"DEBUG (Executor): No final output to process (pipeline might have failed or produced no output).");
                    }
                }
                System.Console.WriteLine($"DEBUG (Executor): --- Statement execution finished ---");

            } // End of loop for all statements
            System.Console.WriteLine($"DEBUG (Executor): All statements executed.");
        }

        /// <summary>
        /// Binds parameters from the parsed command to the cmdlet instance using reflection.
        /// This is called within the context of the specific cmdlet's execution task.
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

                // Get English and potential Arabic names
                string englishParamName = $"-{prop.Name}";
                var arabicNameAttr = prop.GetCustomAttribute<ArabicNameAttribute>();
                string? arabicParamName = arabicNameAttr != null ? $"-{arabicNameAttr.Name}" : null;

                object? valueToSet = null;
                bool found = false;
                string? boundName = null; // Keep track of which name was used for binding
                string? namedValue = null; // The value found via named parameter

                // 1. Try binding by parameter name (Arabic first, then English)
                if (arabicParamName != null && command.Parameters.ContainsKey(arabicParamName))
                {
                    namedValue = command.Parameters[arabicParamName];
                    boundName = arabicParamName;
                    found = true;
                    System.Console.WriteLine($"DEBUG (Binder): Found parameter via Arabic name '{boundName}'.");
                }
                else if (command.Parameters.ContainsKey(englishParamName))
                {
                    namedValue = command.Parameters[englishParamName];
                    boundName = englishParamName;
                    found = true;
                    System.Console.WriteLine($"DEBUG (Binder): Found parameter via English name '{boundName}'.");
                }

                // If found by either name, process the value
                if (found)
                {
                    // Handle boolean switch parameters
                    if (prop.PropertyType == typeof(bool))
                    {
                        // Switch is present if its name exists in the parsed parameters.
                        // Only consider the associated value if it's explicitly true/false.
                        if (!string.IsNullOrEmpty(namedValue)) // Check if parser associated a value
                        {
                            // Try parsing the associated value as bool
                            if (bool.TryParse(namedValue, out bool boolValue))
                            {
                                valueToSet = boolValue; // Assign $true or $false if provided
                            }
                            else
                            {
                                // Any other value associated with a switch is an error
                                throw new ParameterBindingException($"A value '{namedValue}' cannot be specified for the boolean (switch) parameter '{boundName}'.");
                            }
                        }
                        else
                        {
                            // Switch name was present, but no value followed it (or value was empty string)
                            valueToSet = true; // Default behavior for a present switch
                        }
                        // 'found' is already true here
                        System.Console.WriteLine($"DEBUG (Binder): Bound switch parameter '{boundName}' to {valueToSet}.");
                    }
                    // Handle non-boolean named parameters
                    else if (!string.IsNullOrEmpty(namedValue)) // Only bind if parser provided a non-empty value (and it's not a bool prop)
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
                            // 'found' is already true here
                            System.Console.WriteLine($"DEBUG (Binder): Bound named parameter '{boundName}' to value '{valueToSet}' (Type: {prop.PropertyType.Name})");
                        }
                        catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException || ex is NotSupportedException /*TypeConverter might throw this*/)
                        {
                            // Throw specific binding exception for conversion failure
                            throw new ParameterBindingException($"Cannot process argument transformation for parameter '{boundName}'. Cannot convert value \"{namedValue}\" to type \"{prop.PropertyType.FullName}\".", ex)
                            {
                                ParameterName = prop.Name // Still use property name for identification
                            };
                        }
                    }
                    // If found is true but namedValue is null/empty and it's not a bool, it means a named param was provided without a value (e.g., "-Name -OtherParam")
                    // This is generally an error unless it's a switch.
                    else if (string.IsNullOrEmpty(namedValue) && prop.PropertyType != typeof(bool))
                    {
                        throw new ParameterBindingException($"Parameter '{boundName}' requires a value, but none was provided.");
                    }
                    // If found is true, but valueToSet is still null (e.g., switch param where value wasn't explicitly true/false), it's handled above.
                }
                // Note: 'found' now indicates if the parameter was specified by *name* (Arabic or English)

                // 2. Try binding by position (if not found by name and attribute specifies position)
                // Also handle array binding for positional parameters here.
                if (!found && paramAttr.Position >= 0)
                {
                    // Check if it's an array type meant to consume remaining arguments
                    if (prop.PropertyType.IsArray && paramAttr.Position < command.Arguments.Count) // Ensure position is valid
                    {
                        Type? elementType = prop.PropertyType.GetElementType();
                        if (elementType != null)
                        {
                            List<object> arrayValues = new List<object>();
                            bool conversionError = false;
                            int argsConsumed = 0;

                            // Consume all remaining unused arguments from the specified position onwards
                            for (int j = paramAttr.Position; j < command.Arguments.Count; j++)
                            {
                                if (!usedPositionalArgs[j] && command.Arguments[j] is string argValue) // Check if it's a string
                                {
                                    // string argValue = command.Arguments[j]; // Already assigned via pattern matching
                                    try
                                    {
                                        // Attempt conversion
                                        TypeConverter converter = TypeDescriptor.GetConverter(elementType);
                                        object convertedValue = converter != null && converter.CanConvertFrom(typeof(string))
                                            ? converter.ConvertFromString(argValue)! // Assume non-null if conversion succeeds
                                            : Convert.ChangeType(argValue, elementType)!;

                                        arrayValues.Add(convertedValue);
                                        usedPositionalArgs[j] = true; // Mark as used
                                        argsConsumed++;
                                    }
                                    catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException || ex is NotSupportedException)
                                    {
                                        conversionError = true;
                                        // Throw specific binding exception for conversion failure within the array
                                        throw new ParameterBindingException($"Cannot process argument transformation for array parameter '{prop.Name}'. Cannot convert value \"{argValue}\" at index {j} to type \"{elementType.FullName}\".", ex)
                                        {
                                            ParameterName = prop.Name
                                        };
                                    }
                                }
                                // TODO: Handle non-string arguments (subexpressions) for array parameters?
                            }

                            if (!conversionError && argsConsumed > 0) // Only bind if we successfully consumed and converted at least one argument
                            {
                                // Create the typed array and set the value
                                Array finalArray = Array.CreateInstance(elementType, arrayValues.Count);
                                // Copy elements one by one to handle the System.Array type from CreateInstance
                                for (int k = 0; k < arrayValues.Count; k++)
                                {
                                    finalArray.SetValue(arrayValues[k], k);
                                }
                                // arrayValues.CopyTo(finalArray, 0); // This caused type mismatch error
                                valueToSet = finalArray;
                                found = true;
                                System.Console.WriteLine($"DEBUG (Binder): Bound {argsConsumed} remaining positional argument(s) starting at {paramAttr.Position} to array parameter '{prop.Name}' (Type: {prop.PropertyType.Name})");
                            }
                            // If argsConsumed is 0, it means there were no unused args at or after the position, so don't bind.
                        }
                    }
                    // Handle non-array positional parameters (only if not already bound as array)
                    else if (!prop.PropertyType.IsArray && paramAttr.Position < command.Arguments.Count && !usedPositionalArgs[paramAttr.Position])
                    {
                        if (command.Arguments[paramAttr.Position] is string positionalValue) // Check if it's a string
                        {
                            // string positionalValue = command.Arguments[paramAttr.Position]; // Assigned via pattern matching
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
                            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException || ex is NotSupportedException /*TypeConverter might throw this*/)
                            {
                                // Throw specific binding exception for conversion failure
                                throw new ParameterBindingException($"Cannot process argument transformation for parameter '{englishParamName}' at position {paramAttr.Position}. Cannot convert value \"{positionalValue}\" to type \"{prop.PropertyType.FullName}\".", ex)
                                {
                                    ParameterName = prop.Name // Still use property name for identification
                                };
                            }
                        } // End of string check for positionalValue
                        else
                        {
                            // Handle non-string positional argument (e.g., subexpression) - skip binding for now
                            System.Console.WriteLine($"WARN (Binder): Skipping non-string positional argument at index {paramAttr.Position} for parameter '{prop.Name}'. Subexpression execution not implemented.");
                            // Mark as 'used' to prevent array binder from trying it again? Or leave unused? Leave unused for now.
                        }
                    } 
                } 

                // 3. Set the property value if bound (either by name or position) and value is ready
                // Note: 'found' here means bound by *name* OR *position*.
                if (found && valueToSet != null)
                {
                    try
                    {
                        prop.SetValue(cmdlet, valueToSet);
                    }
                    catch (Exception ex)
                    {
                        // This might indicate a problem with the setter logic itself
                        System.Console.WriteLine($"ERROR (Binder): Failed to set property '{prop.Name}': {ex.Message}");
                        throw new ParameterBindingException($"Failed to set property '{prop.Name}'.", ex) { ParameterName = prop.Name };
                    }
                }

                // 4. Check for Mandatory parameters that were not bound by name or position
                if (!found && paramAttr.Mandatory)
                {
                    // Construct error message mentioning both names if applicable
                    string missingParamMsg = $"Missing mandatory parameter '{englishParamName}'";
                    if (arabicParamName != null)
                    {
                        missingParamMsg += $" (or '{arabicParamName}')";
                    }
                    missingParamMsg += $" for cmdlet '{cmdlet.GetType().Name}'.";

                    // Throw an exception to stop execution of this cmdlet's task
                    throw new ParameterBindingException(missingParamMsg)
                    {
                        ParameterName = prop.Name // Store the property name
                    };
                }
            } // <<< End of foreach loop for properties

            // TODO: Handle remaining positional arguments (e.g., pass via pipeline or error)
            // This check might be less relevant now if cmdlets primarily use pipeline input
            // or specifically defined parameters. Consider if unbound arguments should always be an error.
            for (int i = 0; i < command.Arguments.Count; i++)
            {
                if (!usedPositionalArgs[i])
                {
                    // Maybe throw an error here? Or let the cmdlet decide?
                    // Check type before logging
                    if (command.Arguments[i] is string unusedStringArg)
                    {
                         // Don't warn about the TypeLiteral pseudo-arguments
                        if (!unusedStringArg.StartsWith("TypeLiteral:")) {
                             System.Console.WriteLine($"WARN (Binder): Unused positional string argument detected: {unusedStringArg}");
                             // Depending on shell strictness, this could be an error:
                             // throw new ParameterBindingException($"Unexpected positional argument: {unusedStringArg}");
                        }
                    }
                    else // It's likely a parsed subexpression List<ParsedCommand> or other object
                    {
                        System.Console.WriteLine($"WARN (Binder): Unused positional argument of type {command.Arguments[i]?.GetType().Name ?? "null"} detected at index {i}. Subexpression execution not implemented.");
                    }
                }
            }
        }

        /// <summary>
        /// Executes a sub-expression (represented by parsed commands) and returns its output as a single string.
        /// NOTE: This is a simplified implementation. PowerShell has more complex rules for subexpression output conversion.
        /// </summary>
        private static string ExecuteSubExpression(List<ParsedCommand> subCommands)
        {
            // This needs a way to run the executor pipeline but capture the output instead of writing to console/file.
            // For now, return a placeholder string indicating execution is needed.
            // TODO: Implement actual sub-expression execution and output capture.
            System.Console.WriteLine($"DEBUG (Executor): Placeholder execution for subexpression starting with '{subCommands.FirstOrDefault()?.CommandName ?? "N/A"}'.");
            
            // --- Placeholder Logic ---
            // In a real implementation:
            // 1. Create temporary input/output BlockingCollections for the sub-pipeline.
            // 2. Call a modified version of the main Execute loop logic for the subCommands list,
            //    directing the final output to a temporary collection instead of console/file.
            // 3. Wait for the sub-pipeline tasks to complete.
            // 4. Collect all PipelineObjects from the temporary output collection.
            // 5. Convert the collected objects to a string (e.g., join with spaces or newlines).
            // 6. Return the resulting string.

            return $"[SubExprResult:{subCommands.FirstOrDefault()?.CommandName ?? "Empty"}]"; 
        }

    }
}
