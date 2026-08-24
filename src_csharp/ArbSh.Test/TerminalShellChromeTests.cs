using ArbSh.Terminal.Models;
using ArbSh.Terminal.ViewModels;

namespace ArbSh.Test;

public sealed class TerminalShellChromeTests
{
    [Fact]
    public void ConstructorExposesCompactArabicWelcomeAndSessionStatus()
    {
        string workingDirectory = Path.GetFullPath(Path.GetTempPath());
        var viewModel = new MainWindowViewModel(workingDirectory);

        Assert.Equal(workingDirectory, viewModel.CurrentDirectory);
        Assert.Equal("جاهز", viewModel.StatusText);
        Assert.Equal("أربش> ", viewModel.Prompt);
        Assert.Collection(
            viewModel.Lines,
            line =>
            {
                Assert.Equal(TerminalLineKind.System, line.Kind);
                Assert.Equal("مرحبًا بك في أربش", line.Text);
            },
            line =>
            {
                Assert.Equal(TerminalLineKind.System, line.Kind);
                Assert.Contains("مساعدة", line.Text, StringComparison.Ordinal);
            });
        Assert.DoesNotContain(viewModel.Lines, line => line.Text.Contains(workingDirectory, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ChangeDirectoryRefreshesChromePathAndReturnsToReadyState()
    {
        string root = Path.Combine(Path.GetTempPath(), $"أربش واجهة {Guid.NewGuid():N}");
        string child = Path.Combine(root, "مجلد عربي");
        Directory.CreateDirectory(child);

        try
        {
            var viewModel = new MainWindowViewModel(root);
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            await viewModel.SubmitInputAsync($"انتقل '{child}'");

            Assert.Equal(Path.GetFullPath(child), viewModel.CurrentDirectory);
            Assert.Equal("جاهز", viewModel.StatusText);
            Assert.Contains(nameof(MainWindowViewModel.CurrentDirectory), changedProperties);
            Assert.Contains(nameof(MainWindowViewModel.StatusText), changedProperties);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
