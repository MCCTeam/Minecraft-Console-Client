using MinecraftClient.Scripting;

namespace MinecraftClient.Tests;

public sealed class CSharpRunnerTests
{
    [Theory]
    [InlineData("//using MinecraftClient.CommandHandler", "using MinecraftClient.CommandHandler;")]
    [InlineData("//using MinecraftClient.CommandHandler;", "using MinecraftClient.CommandHandler;")]
    public void NormalizeUsingDirectiveAddsMissingSemicolon(string directive, string expected)
    {
        Assert.Equal(expected, CSharpRunner.NormalizeUsingDirective(directive));
    }
}
