using System; // Added for ArgumentNullException
using System.Collections.Generic;
using System.Collections.Concurrent; // Using ConcurrentQueue for potential thread safety later

namespace ArbSh.Console
{
    /// <summary>
    /// Base class for all cmdlets in ArbSh.
    /// Provides the basic structure for processing input and producing output.
    /// Inspired by System.Management.Automation.Cmdlet.
    /// </summary>
    public abstract class CmdletBase
    {
        // --- Internal Properties for Execution ---

        /// <summary>
        /// Internal collection to store objects written by WriteObject.
        /// This will be consumed by the next stage in the pipeline or the host.
        /// Using ConcurrentQueue for potential future parallel execution scenarios.
        /// </summary>
        internal BlockingCollection<PipelineObject> OutputCollection { get; set; } = new BlockingCollection<PipelineObject>();

        // --- Lifecycle Methods ---

        /// <summary>
        /// Method called once at the beginning of pipeline processing for this cmdlet instance.
        /// </summary>
        public virtual void BeginProcessing() { }

        /// <summary>
        /// Method called for each object coming from the pipeline.
        /// </summary>
        /// <param name="inputObject">The input object from the pipeline.</param>
        public virtual void ProcessRecord(PipelineObject inputObject) { }

        /// <summary>
        /// Method called once after all pipeline input has been processed.
        /// Used for cmdlets that need to perform an action after collecting all input.
        /// </summary>
        public virtual void EndProcessing() { }

        /// <summary>
        /// Writes an object to the output pipeline (internal collection).
        /// </summary>
        /// <param name="outputObject">The object to write.</param>
        protected void WriteObject(object? outputObject)
        {
            // Wrap the raw object in a PipelineObject if it isn't already
            PipelineObject pipelineObject = outputObject is PipelineObject po ? po : new PipelineObject(outputObject);

            if (OutputCollection == null)
            {
                // This shouldn't happen if the executor sets it up correctly
                throw new InvalidOperationException("Output collection is not initialized.");
            }
            OutputCollection.Add(pipelineObject);
        }

        // TODO: Add methods for writing errors, warnings, verbose output, etc.
        // TODO: Add mechanisms for parameter binding.
        // TODO: Add access to session state, variables, etc.
    }
}
