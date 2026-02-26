namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يعرض المجلد الحالي للجلسة.
    /// </summary>
    [ArabicName("المسار")]
    public sealed class GetCurrentDirectoryCmdlet : CmdletBase
    {
        /// <inheritdoc />
        public override void EndProcessing()
        {
            WriteObject(ShellSessionContext.CurrentDirectory);
        }
    }
}
