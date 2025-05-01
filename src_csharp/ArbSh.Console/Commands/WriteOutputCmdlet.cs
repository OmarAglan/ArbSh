using System;

namespace ArbSh.Console.Commands
{
    /// <summary>
    /// Basic implementation of a cmdlet similar to PowerShell's Write-Output.
    /// Writes objects to the pipeline/console.
    /// </summary>
    public class WriteOutputCmdlet : CmdletBase
    {
        /// <summary>
        /// The object(s) to write to the output stream.
        /// Can be bound from pipeline input or direct argument.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, HelpMessage = "The object(s) to write to the output stream.")]
        public object? InputObject { get; set; }

        private bool _processedInputObjectParam = false;

        /// <summary>
        /// Processes each record (object) coming from the pipeline, or the InputObject parameter if provided directly.
        /// </summary>
        /// <param name="input">The object from the pipeline (null if no pipeline input for this call).</param>
        public override void ProcessRecord(PipelineObject? input)
        {
            if (input != null)
            {
                // Priority to pipeline input for this specific record
                WriteObject(input.Value);
                _processedInputObjectParam = true; // Mark that we processed something (even if it was pipeline)
            }
            else if (InputObject != null && !_processedInputObjectParam)
            {
                // If no pipeline input for this call, AND InputObject parameter was bound, AND we haven't processed it yet
                WriteObject(InputObject);
                _processedInputObjectParam = true; // Mark as processed to avoid writing it multiple times if Begin/EndProcessing are called
            }
            // If input is null and InputObject is null, do nothing for this record.
        }

        // Reset flag in case the cmdlet instance is reused (though currently they are recreated each time)
        public override void BeginProcessing()
        {
            _processedInputObjectParam = false;
        }

        // No specific EndProcessing needed.
    }
}
