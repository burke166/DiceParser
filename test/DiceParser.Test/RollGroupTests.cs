using DiceParser;
using DiceParser.Exceptions;

namespace DiceParser.Test;

public class RollGroupTests
{
    private const int FixedSeed = 0xBEE5;

    [Fact]
    public void Labeled_attack_and_damage_group_evaluates_each_subexpression()
    {
        var engine = new DiceEngine(FixedSeed);
        var outcome = Assert.IsType<RollGroupExpressionResult>(Assert.Single(engine.Execute("{attack: 1d20+5, damage: 2d6+3}")));

        Assert.Equal(2, outcome.Group.Results.Count);
        Assert.Equal("attack", outcome.Group.Results[0].Label);
        Assert.Equal("damage", outcome.Group.Results[1].Label);
        Assert.Single(outcome.Group.Results[0].Result.Rolls);
        Assert.Equal(2, outcome.Group.Results[1].Result.Rolls.Length);
        Assert.InRange(outcome.Group.Results[0].Result.Rolls[0], 1, 20);
        Assert.All(outcome.Group.Results[1].Result.Rolls, v => Assert.InRange(v, 1, 6));
        Assert.Equal(outcome.Group.Results[0].Result.Rolls[0] + 5, outcome.Group.Results[0].Result.Total);
        Assert.Equal(outcome.Group.Results[1].Result.Rolls.Sum() + 3, outcome.Group.Results[1].Result.Total);
    }

    [Fact]
    public void Single_entry_to_hit_hyphenated_save_dc_and_damage_examples_parse()
    {
        var engine = new DiceEngine(3);
        var g1 = Assert.IsType<RollGroupExpressionResult>(Assert.Single(engine.Execute("{to_hit: 1d20+7}"))).Group;
        Assert.Single(g1.Results);
        Assert.Equal("to_hit", g1.Results[0].Label);

        var g2 = Assert.IsType<RollGroupExpressionResult>(
            Assert.Single(engine.Execute("{save-dc: 8+3+4, damage: 1d8+3}"))).Group;
        Assert.Equal(2, g2.Results.Count);
        Assert.Equal("save-dc", g2.Results[0].Label);
        Assert.Equal(15, g2.Results[0].Result.Total);
        Assert.Equal("damage", g2.Results[1].Label);
    }

    [Fact]
    public void Semicolon_program_may_mix_numeric_and_roll_group_expressions()
    {
        var engine = new DiceEngine(FixedSeed);
        var results = engine.Execute("1d20+5; {attack: 1d20+5, damage: 2d6+3}").ToList();
        Assert.Equal(2, results.Count);
        _ = Assert.IsType<NumericExpressionResult>(results[0]);
        _ = Assert.IsType<RollGroupExpressionResult>(results[1]);
    }

    [Fact]
    public void Empty_group_throws()
    {
        var engine = new DiceEngine(1);
        var ex = Assert.Throws<ParseException>(() => _ = engine.Execute("{}"));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_label_before_colon_throws()
    {
        var engine = new DiceEngine(1);
        Assert.Throws<ParseException>(() => _ = engine.Execute("{: 1d20}"));
    }

    [Fact]
    public void Missing_expression_after_colon_throws()
    {
        var engine = new DiceEngine(1);
        Assert.Throws<ParseException>(() => _ = engine.Execute("{attack:}"));
    }

    [Fact]
    public void Duplicate_labels_throw()
    {
        var engine = new DiceEngine(1);
        var ex = Assert.Throws<ParseException>(() => _ = engine.Execute("{attack: 1d20, attack: 2}"));
        Assert.Contains("Duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Trailing_comma_throws()
    {
        var engine = new DiceEngine(1);
        Assert.Throws<ParseException>(() => _ = engine.Execute("{attack: 1d20,}"));
    }

    [Fact]
    public void Semicolon_inside_group_throws()
    {
        var engine = new DiceEngine(1);
        Assert.Throws<ParseException>(() => _ = engine.Execute("{attack: 1d20; damage: 2d6}"));
    }

    [Fact]
    public void Roll_group_used_as_numeric_operand_throws_at_eval()
    {
        var engine = new DiceEngine(1);
        Assert.Throws<EvalException>(() => _ = engine.Execute("1+{a: 1d6}"));
    }

    [Fact]
    public void Custom_die_after_d_still_parses_value_only_braces()
    {
        var engine = new DiceEngine(FixedSeed);
        var r = engine.ExecuteSingleNumericRoll("1d{-1,0,1}");
        Assert.Single(r.Rolls);
        Assert.Contains(r.Rolls[0], new[] { -1, 0, 1 });
    }
}
