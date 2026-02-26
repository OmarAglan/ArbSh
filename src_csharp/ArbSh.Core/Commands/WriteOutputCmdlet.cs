using System;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يكتب النص/الكائن إلى مجرى المخرجات.
    /// </summary>
    [ArabicName("اطبع")]
    public class WriteOutputCmdlet : CmdletBase
    {
        /// <summary>
        /// النص (أو الكائن) المراد طباعته.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, HelpMessage = "النص أو الكائن المراد طباعته.")]
        [ArabicName("النص")]
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

