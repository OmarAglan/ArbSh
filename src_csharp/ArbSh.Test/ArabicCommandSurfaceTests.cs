using ArbSh.Core;

namespace ArbSh.Test;

public sealed class ArabicCommandSurfaceTests
{
    [Theory]
    [InlineData("مساعدة")]
    [InlineData("الأوامر")]
    [InlineData("اطبع")]
    [InlineData("اختبار-مصفوفة")]
    [InlineData("اختبار-نوع")]
    [InlineData("انتقل")]
    [InlineData("المسار")]
    [InlineData("اعرض")]
    public void Find_ResolvesArabicCommandNames(string commandName)
    {
        Type? cmdletType = CommandDiscovery.Find(commandName);
        Assert.NotNull(cmdletType);
    }

    [Theory]
    [InlineData("Get-Help")]
    [InlineData("Get-Command")]
    [InlineData("Write-Output")]
    [InlineData("احصل-مساعدة")]
    [InlineData("Test-Array-Binding")]
    [InlineData("Test-Type-Literal")]
    public void Find_DoesNotResolveLegacyCommandNames(string commandName)
    {
        Type? cmdletType = CommandDiscovery.Find(commandName);
        Assert.Null(cmdletType);
    }

    [Fact]
    public void GetAllCommands_ContainsOnlyActiveArabicSurface()
    {
        IReadOnlyDictionary<string, Type> commands = CommandDiscovery.GetAllCommands();

        Assert.Contains("مساعدة", commands.Keys);
        Assert.Contains("الأوامر", commands.Keys);
        Assert.Contains("اطبع", commands.Keys);
        Assert.Contains("اختبار-مصفوفة", commands.Keys);
        Assert.Contains("اختبار-نوع", commands.Keys);
        Assert.Contains("انتقل", commands.Keys);
        Assert.Contains("المسار", commands.Keys);
        Assert.Contains("اعرض", commands.Keys);
        Assert.DoesNotContain("Get-Help", commands.Keys);
        Assert.DoesNotContain("Get-Command", commands.Keys);
        Assert.DoesNotContain("Write-Output", commands.Keys);
        Assert.DoesNotContain("احصل-مساعدة", commands.Keys);
        Assert.DoesNotContain("Test-Array-Binding", commands.Keys);
        Assert.DoesNotContain("Test-Type-Literal", commands.Keys);
    }
}
