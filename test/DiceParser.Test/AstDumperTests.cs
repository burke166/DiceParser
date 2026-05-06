using DiceParser.Diagnostics;

namespace DiceParser.Test;

public class AstDumperTests
{
    [Fact]
    public void DumpProgram_single_numeric_expression_includes_dice_and_number_nodes()
    {
        var dumps = AstDumper.DumpProgram("2d6+1");

        Assert.Single(dumps);
        Assert.Contains("Binary Add", dumps[0], StringComparison.Ordinal);
        Assert.Contains("Dice mods#0", dumps[0], StringComparison.Ordinal);
        Assert.Contains("Number 2", dumps[0], StringComparison.Ordinal);
        Assert.Contains("Number 6", dumps[0], StringComparison.Ordinal);
        Assert.Contains("Number 1", dumps[0], StringComparison.Ordinal);
    }

    [Fact]
    public void DumpProgram_roll_group_includes_labels()
    {
        var dumps = AstDumper.DumpProgram("{atk:1d20,damage:2d6+3}");

        Assert.Single(dumps);
        Assert.Contains("RollGroup", dumps[0], StringComparison.Ordinal);
        Assert.Contains("atk:", dumps[0], StringComparison.Ordinal);
        Assert.Contains("damage:", dumps[0], StringComparison.Ordinal);
    }

    [Fact]
    public void DumpProgram_program_segments_match_semicolon_splits()
    {
        var dumps = AstDumper.DumpProgram("1d6;2d8");

        Assert.Equal(2, dumps.Count);
        Assert.Contains("Number 1", dumps[0], StringComparison.Ordinal);
        Assert.Contains("Number 6", dumps[0], StringComparison.Ordinal);
        Assert.Contains("Number 2", dumps[1], StringComparison.Ordinal);
        Assert.Contains("Number 8", dumps[1], StringComparison.Ordinal);
    }
}
