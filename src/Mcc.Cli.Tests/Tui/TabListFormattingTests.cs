using Mcc.Cli.Tui.Presentation;
using Mcc.Cli.Tui;
using DMCBK.Core;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// Unit tests for the pure tab-list presentation logic: the 5-bar ping gauge, team-label/opaque-name detection, team-aware sort, and multi-column layout math.
/// Ported from legacy's <c>TabList/TabListFormatter.cs</c>.
/// No session dependency.
/// </summary>
public sealed class TabListFormattingTests
{
    [Theory]
    [InlineData(-1, "Unknown", 0)]
    [InlineData(0, "Excellent", 5)]
    [InlineData(149, "Excellent", 5)]
    [InlineData(150, "Good", 4)]
    [InlineData(300, "Fair", 3)]
    [InlineData(600, "Poor", 2)]
    [InlineData(999, "Poor", 2)]
    [InlineData(1000, "Bad", 1)]
    public void PingGauge_MatchesLegacyThresholds(int pingMs, string expectedTier, int expectedBars)
    {
        Assert.Equal(expectedTier, TabListFormatting.GetPingTier(pingMs).ToString());
        Assert.Equal(expectedBars, TabListFormatting.GetFilledBars(pingMs));
    }

    [Fact]
    public void FormatPingMs_UnmeasuredIsQuestionMarks()
        => Assert.Equal("???ms", TabListFormatting.FormatPingMs(-1));

    [Fact]
    public void FormatPingMs_CapsAtFourDigits()
        => Assert.Equal("9999ms", TabListFormatting.FormatPingMs(50000));

    [Theory]
    [InlineData("Spectator", true)]
    [InlineData("Survival", false)]
    [InlineData("spectator", true)]
    public void IsSpectator_MatchesCaseInsensitively(string gameMode, bool expected)
        => Assert.Equal(expected, TabListFormatting.IsSpectator(gameMode));

    [Fact]
    public void LooksLikeOpaqueTeamName_DetectsGuid()
        => Assert.True(TabListFormatting.LooksLikeOpaqueTeamName(Guid.NewGuid().ToString()));

    [Fact]
    public void LooksLikeOpaqueTeamName_DetectsHyphenHeavyGeneratedId()
        => Assert.True(TabListFormatting.LooksLikeOpaqueTeamName("team-generated-abc-def-ghijklmno"));

    [Fact]
    public void LooksLikeOpaqueTeamName_ShortHumanNameIsNotOpaque()
        => Assert.False(TabListFormatting.LooksLikeOpaqueTeamName("red"));

    [Fact]
    public void TeamLabel_PrefersDisplayNameOverInternalName()
    {
        var team = new TeamInfo("red_team", "Red Team", string.Empty, string.Empty, 12, []);
        Assert.Equal("Red Team", TabListFormatting.TeamLabel(team));
    }

    [Fact]
    public void TeamLabel_FallsBackToNameWhenNotOpaque()
    {
        var team = new TeamInfo("red", "red", string.Empty, string.Empty, 12, []);
        Assert.Equal("red", TabListFormatting.TeamLabel(team));
    }

    [Fact]
    public void TeamLabel_OpaqueGeneratedNameRendersEmpty()
    {
        string opaque = Guid.NewGuid().ToString();
        var team = new TeamInfo(opaque, opaque, string.Empty, string.Empty, 12, []);
        Assert.Equal(string.Empty, TabListFormatting.TeamLabel(team));
    }

    [Fact]
    public void TeamLabel_NullTeamIsEmpty()
        => Assert.Equal(string.Empty, TabListFormatting.TeamLabel(null));

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(15)]
    public void TeamColor_NamedIndicesResolve(int index)
        => Assert.NotNull(TabListFormatting.TeamColor(index));

    [Theory]
    [InlineData(21)]
    [InlineData(-1)]
    [InlineData(16)]
    public void TeamColor_UnnamedIndicesReturnNull(int index)
        => Assert.Null(TabListFormatting.TeamColor(index));

    [Fact]
    public void IndexTeamsByMember_MapsEachMemberName()
    {
        var red = new TeamInfo("red", "Red", string.Empty, string.Empty, 12, ["Alice", "Bob"]);
        var blue = new TeamInfo("blue", "Blue", string.Empty, string.Empty, 9, ["Carol"]);
        IReadOnlyDictionary<string, TeamInfo> map = TabListFormatting.IndexTeamsByMember([red, blue]);

        Assert.Equal("red", map["Alice"].Name);
        Assert.Equal("red", map["Bob"].Name);
        Assert.Equal("blue", map["Carol"].Name);
        Assert.False(map.ContainsKey("Dave"));
    }

    [Fact]
    public void Sort_GroupsByTeamThenName_WithSpectatorsLast()
    {
        TabListEntryInfo Entry(string name, string gameMode) => new(Guid.NewGuid(), name, gameMode, 50, null, true, 0);

        var entries = new List<TabListEntryInfo>
        {
            Entry("Zed", "Survival"),   // no team
            Entry("Bob", "Survival"),   // team red
            Entry("Alice", "Survival"), // team red
            Entry("Ghost", "Spectator"), // no team, spectator: sorts last
            Entry("Carol", "Survival"), // no team
        };

        var red = new TeamInfo("red", "Red", string.Empty, string.Empty, 12, ["Bob", "Alice"]);
        IReadOnlyDictionary<string, TeamInfo> teamByMember = TabListFormatting.IndexTeamsByMember([red]);

        IReadOnlyList<TabListEntryInfo> sorted = TabListFormatting.Sort(entries, teamByMember);

        // Non-spectators first (team "" sorts before team "red" alphabetically), spectators last regardless of team.
        Assert.Equal(["Carol", "Zed", "Alice", "Bob", "Ghost"], sorted.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void Sort_DropsUnlistedAndBlankNamedEntries()
    {
        var entries = new List<TabListEntryInfo>
        {
            new(Guid.NewGuid(), "Visible", "Survival", 10, null, true, 0),
            new(Guid.NewGuid(), "Hidden", "Survival", 10, null, false, 0),
            new(Guid.NewGuid(), "  ", "Survival", 10, null, true, 0),
        };

        IReadOnlyList<TabListEntryInfo> sorted = TabListFormatting.Sort(entries, TabListFormatting.IndexTeamsByMember([]));
        Assert.Single(sorted);
        Assert.Equal("Visible", sorted[0].Name);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(40, 2)]
    [InlineData(41, 3)]
    public void ColumnCount_GrowsPastTwentyRows(int entryCount, int expectedColumns)
        => Assert.Equal(expectedColumns, TabListFormatting.ColumnCount(entryCount));

    [Fact]
    public void RowsPerColumn_SplitsEvenly()
        => Assert.Equal(11, TabListFormatting.RowsPerColumn(21, 2));
}
