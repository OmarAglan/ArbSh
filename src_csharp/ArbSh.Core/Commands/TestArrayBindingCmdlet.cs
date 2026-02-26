using System;
using System.Linq;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// Simple cmdlet to test positional array binding.
    /// </summary>
    public class TestArrayBindingCmdlet : CmdletBase
    {
        [Parameter(Position = 0, HelpMessage = "Accepts multiple string arguments.")]
        public string[]? InputStrings { get; set; }

        [Parameter(HelpMessage = "An optional switch.")]
        public bool MySwitch { get; set; }

        public override void ProcessRecord(PipelineObject? input)
        {
            // This cmdlet primarily works with parameters bound before ProcessRecord is called.
            // We'll output the bound array in EndProcessing.
        }

        public override void EndProcessing()
        {
            WriteObject($"MySwitch value: {MySwitch}");
            if (InputStrings != null && InputStrings.Any())
            {
                WriteObject($"Received {InputStrings.Length} strings:");
                for (int i = 0; i < InputStrings.Length; i++)
                {
                    WriteObject($"  [{i}]: '{InputStrings[i]}'");
                }
            }
            else
            {
                WriteObject("Received no strings.");
            }
        }
    }
}

