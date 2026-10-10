// DotEnv tests: parsing rules and load semantics (explicit environment wins, missing file is silent).

using Mcc.Cli.Configuration;
using Xunit;

namespace Mcc.Cli.Tests.Configuration;

public sealed class DotEnvTests
{
    [Fact]
    public void Parse_ReadsPairsAndSkipsNoise()
    {
        IReadOnlyList<KeyValuePair<string, string>> pairs = DotEnv.Parse(
            "# comment\nMCC_A=1\n\nexport MCC_B=two\nbad line\n123=no\nMCC_C=a=b\n");

        Assert.Equal(3, pairs.Count);
        Assert.Equal("1", pairs[0].Value);
        Assert.Equal("MCC_B", pairs[1].Key);
        Assert.Equal("two", pairs[1].Value);
        Assert.Equal("a=b", pairs[2].Value);
    }

    [Fact]
    public void Parse_StripsMatchingQuotes()
    {
        IReadOnlyList<KeyValuePair<string, string>> pairs = DotEnv.Parse(
            "MCC_A=\"quoted\"\nMCC_B='single'\nMCC_C=\"mismatch'\n");

        Assert.Equal("quoted", pairs[0].Value);
        Assert.Equal("single", pairs[1].Value);
        Assert.Equal("\"mismatch'", pairs[2].Value);
    }

    [Fact]
    public void Load_SetsMissingVariablesAndLeavesExplicitOnesAlone()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(
                Path.Combine(dir, ".env"), "MCC_DOTENV_TEST_NEW=from-file\nMCC_DOTENV_TEST_OLD=from-file\n");
            Environment.SetEnvironmentVariable("MCC_DOTENV_TEST_OLD", "explicit");

            Assert.Equal(1, DotEnv.Load(dir));
            Assert.Equal("from-file", Environment.GetEnvironmentVariable("MCC_DOTENV_TEST_NEW"));
            Assert.Equal("explicit", Environment.GetEnvironmentVariable("MCC_DOTENV_TEST_OLD"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_DOTENV_TEST_NEW", null);
            Environment.SetEnvironmentVariable("MCC_DOTENV_TEST_OLD", null);
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFileLoadsNothing()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.Equal(0, DotEnv.Load(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
