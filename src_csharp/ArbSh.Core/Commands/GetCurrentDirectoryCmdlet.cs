namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يعرض المجلد الحالي للجلسة.
    /// </summary>
    [ArabicName("المسار")]
    [ArabicDescription("يعرض مسار مجلد العمل الحالي.")]
    public sealed class GetCurrentDirectoryCmdlet : CmdletBase
    {
        /// <inheritdoc />
        public override void EndProcessing()
        {
            WriteObject(ShellSessionContext.CurrentDirectory);
        }
    }
}
