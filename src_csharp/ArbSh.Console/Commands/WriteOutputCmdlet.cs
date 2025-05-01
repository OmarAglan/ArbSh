using System;

namespace ArbSh.Console.Commands
{
    /// <summary>
    /// Basic implementation of a cmdlet similar to PowerShell's Write-Output.
    /// Writes objects to the pipeline/console.
    /// </summary>
    public class WriteOutputCmdlet : CmdletBase
    {
        // TODO: Add parameter support (e.g., -InputObject)

        /// <summary>
        /// Processes each record (object) coming from the pipeline.
        /// </summary>
        /// <param name="inputObject">The object from the pipeline.</param>
        public override void ProcessRecord(PipelineObject inputObject)
        {
            // Simply write the input object to the output.
            // The base WriteObject currently just prints to console.
            WriteObject(inputObject.Value);
        }

        // No specific BeginProcessing or EndProcessing needed for this simple cmdlet.
    }
}
