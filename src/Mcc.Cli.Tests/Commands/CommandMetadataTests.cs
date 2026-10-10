using System.Globalization;
using System.Reflection;
using DMCBK.Core.Commands;
using DMCBK.Core.Commands.Impl;
using Xunit;

namespace Mcc.Cli.Tests.Commands;

/// <summary>
/// The rules a command's help metadata has to keep, enforced over every command in the tree rather than checked by eye.
/// Each one here fired on real code before it was written.
/// </summary>
public sealed class CommandMetadataTests
{
    /// <summary>Every built-in command, instantiated the way <c>CommandService</c> instantiates them.</summary>
    public static TheoryData<string> CommandNames()
    {
        var data = new TheoryData<string>();
        foreach (CommandBase command in Commands())
            data.Add(command.CmdName);

        return data;
    }

    private static IReadOnlyList<CommandBase> Commands()
    {
        var commands = new List<CommandBase>();
        foreach (Type type in typeof(HelpCommand).Assembly.GetTypes().Concat(typeof(DMCBK.Core.Commands.Impl.ScriptsCommand).Assembly.GetTypes()))
        {
            if (type.IsAbstract || !typeof(CommandBase).IsAssignableFrom(type))
                continue;

            // RemovedCommand has only private constructors and static factories; it is covered by its own assertion below rather than by the general sweep.
            if (type.GetConstructor(Type.EmptyTypes) is not { } ctor)
                continue;

            commands.Add((CommandBase)ctor.Invoke(null));
        }

        return commands;
    }

    private static CommandBase Find(string name)
        => Commands().Single(c => c.CmdName == name);

    /// <summary>
    /// <c>CmdUsage</c> is a grammar, not a sentence.
    /// <c>CommandBase.BuildUsage</c> prefixes it with the command character, so a sentence rendered as <c>/Basic usage: /inventory ...: Inventory command</c> on the real <c>/help inventory</c> page.
    /// </summary>
    [Theory]
    [MemberData(nameof(CommandNames))]
    public void CmdUsage_IsAGrammar_NotASentence(string name)
    {
        CommandBase command = Find(name);
        string usage = command.CmdUsage;

        Assert.False(usage.Contains(": ", StringComparison.Ordinal),
            $"{name}: CmdUsage reads as a sentence ('{usage}'). It is prefixed with the command char and " +
            "suffixed with CmdDesc, so a colon produces '/Some sentence: ...: Description'.");
        Assert.False(usage.EndsWith('.'), $"{name}: CmdUsage ends with a full stop ('{usage}').");
        Assert.StartsWith(command.CmdName, usage, StringComparison.Ordinal);
    }

    /// <summary>
    /// Descriptions are read as a column in the grouped index, so they are sentence-case and punctuated.
    /// The legacy corpus capitalised some and not others, which is invisible in a flat grammar dump and obvious in a column.
    /// </summary>
    [Theory]
    [MemberData(nameof(CommandNames))]
    public void CmdDesc_IsSentenceCase_AndPunctuated(string name)
    {
        string desc = Find(name).CmdDesc;
        if (desc.Length == 0)
            return;

        Assert.True(char.IsUpper(desc[0]) || !char.IsLetter(desc[0]),
            $"{name}: description starts lowercase ('{desc}').");
        Assert.True(desc.TrimEnd()[^1] is '.' or '!' or '?',
            $"{name}: description has no terminal punctuation ('{desc}').");
    }

    /// <summary>An example has to be runnable: it starts with the command it is an example of.</summary>
    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Examples_StartWithTheirCommand(string name)
    {
        CommandBase command = Find(name);
        foreach (string example in command.Examples)
        {
            Assert.True(
                example.StartsWith(command.CmdName, StringComparison.Ordinal),
                $"{name}: example '{example}' does not start with the command name.");
            Assert.False(example.StartsWith('/'), $"{name}: example '{example}' includes a prefix character; " +
                "the renderer adds the active prefix, which is configurable.");
        }
    }

    /// <summary>A cross-reference to a command that does not exist is a dead end on a help page.</summary>
    [Fact]
    public void SeeAlso_NamesRealCommands()
    {
        IReadOnlyList<CommandBase> commands = Commands();
        HashSet<string> known = [.. commands.Select(c => c.CmdName)];

        // The three host commands are registered by Mcc.Cli, which this assembly cannot see, so they are named here rather than discovered.
        known.UnionWith(["clear-console", "console-chat", "exit", "minimap", "maps"]);

        foreach (CommandBase command in commands)
        {
            foreach (string other in command.SeeAlso)
            {
                Assert.True(known.Contains(other),
                    $"{command.CmdName}: SeeAlso names '{other}', which is not a registered command.");
            }
        }
    }

    /// <summary>A <c>ManTopic</c> pointing at a page that does not exist renders a link to nothing.</summary>
    [Fact]
    public void ManTopic_NamesARealManualTopic()
    {
        foreach (CommandBase command in Commands())
        {
            if (command.ManTopic is not { Length: > 0 } topic)
                continue;

            Assert.True(DMCBK.Core.Manual.ManualTopics.Find(topic) is not null,
                $"{command.CmdName}: ManTopic '{topic}' is not a manual topic.");
        }
    }

    /// <summary>
    /// Every command lands in a real category.
    /// The default is <see cref="CommandCategory.Plugins"/>, which is correct for a plugin and wrong for a built-in, so a built-in that forgot to declare one shows up under PLUGINS in the index.
    /// </summary>
    [Fact]
    public void BuiltInCommands_DeclareACategory()
    {
        foreach (CommandBase command in Commands())
        {
            Assert.False(
                command.Category == CommandCategory.Plugins,
                $"{command.CmdName}: built-in command has no Category, so it lists under PLUGINS.");
        }
    }

    /// <summary>The removed commands answer when typed but must not advertise themselves in the index.</summary>
    [Fact]
    public void RemovedCommands_AreHiddenFromTheIndex()
    {
        Assert.False(RemovedCommand.Upgrade().ShowInIndex);
        Assert.False(RemovedCommand.Script().ShowInIndex);
    }

    /// <summary>
    /// <c>tryout</c> was removed outright, not left as a tombstone: no factory, no registration, nothing in the tree.
    /// </summary>
    [Fact]
    public void TryoutCommand_IsGone()
    {
        Assert.Null(typeof(RemovedCommand).GetMethod("Tryout", BindingFlags.Public | BindingFlags.Static));
        Assert.DoesNotContain(Commands(), c => c.CmdName == "tryout");
    }

    /// <summary>A usage row's syntax must not repeat the command name; the renderer prints it already.</summary>
    [Theory]
    [MemberData(nameof(CommandNames))]
    public void UsageLines_DoNotRepeatTheCommandName(string name)
    {
        CommandBase command = Find(name);
        foreach (UsageLine row in command.UsageLines)
        {
            Assert.False(
                row.Syntax.StartsWith(command.CmdName + " ", StringComparison.Ordinal),
                $"{name}: usage row '{row.Syntax}' repeats the command name.");
            Assert.False(row.Syntax.StartsWith('/'),
                $"{name}: usage row '{row.Syntax}' includes a prefix character.");
        }
    }

    /// <summary>
    /// The invariant behind <c>/debug state</c>'s coordinate triple: on a comma-decimal locale it read "0,00, 0,00, 0,00", six numbers to the eye.
    /// Formatting there is invariant now.
    /// </summary>
    [Fact]
    public void CommandStrings_FormatInvariantly()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Contains(".", CommandStrings.TpsCurrent(19.5), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
