using DiceParser;
using DiceParser.Exceptions;
using DiceParser.Random;

namespace DiceParser.Test;

/// <summary>Fudge/Fate <c>dF</c> shorthand and parity with <c>d&#123;-1,0,1&#125;</c>.</summary>
public class FudgeDiceTests
{
    [Fact]
    public void Four_dF_records_four_rolls_each_in_minus_one_zero_one()
    {
        var engine = new DiceEngine(0xF00D);
        int[] allowed = [-1, 0, 1];
        var r = engine.ExecuteSingleNumericRoll("4dF");
        Assert.Equal(4, r.Rolls.Length);
        Assert.Equal(4, r.DiceRolled);
        Assert.All(r.Rolls, v => Assert.Contains(v, allowed));
        Assert.Equal(r.Rolls.Sum(), r.Total);
    }

    [Fact]
    public void Four_dfkh3_parses_fudge_then_keep_highest()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("4dfkh3", new QueueRng(0, 1, 2, 2));
        Assert.Equal([-1, 0, 1, 1], r.Rolls);
        Assert.Equal(2, r.Total);
    }

    [Fact]
    public void Four_df_matches_four_dF_with_same_seed()
    {
        var upper = new DiceEngine(42);
        var lower = new DiceEngine(42);
        var a = upper.ExecuteSingleNumericRoll("4dF");
        var b = lower.ExecuteSingleNumericRoll("4df");
        Assert.Equal(a.Total, b.Total);
        Assert.Equal(a.Rolls, b.Rolls);
        Assert.Equal(a.DiceRolled, b.DiceRolled);
    }

    [Fact]
    public void Four_dF_total_is_always_between_minus_four_and_four()
    {
        var engine = new DiceEngine(0);
        for (int seed = 0; seed < 500; seed++)
        {
            var e = new DiceEngine(seed);
            var r = e.ExecuteSingleNumericRoll("4dF");
            Assert.InRange(r.Total, -4, 4);
        }
    }

    [Fact]
    public void Four_dF_matches_four_custom_fudge_die_under_same_rng_indices()
    {
        var engine = new DiceEngine(0);
        int[] indices = [2, 0, 1, 2];
        var a = engine.ExecuteWithRngSingleNumeric("4dF", new QueueRng(indices));
        var b = engine.ExecuteWithRngSingleNumeric("4d{-1,0,1}", new QueueRng(indices));
        Assert.Equal(b.Total, a.Total);
        Assert.Equal(b.Rolls, a.Rolls);
        Assert.Equal(b.DiceRolled, a.DiceRolled);
    }

    [Fact]
    public void Implicit_dF_matches_one_dF_under_same_rng()
    {
        var engine = new DiceEngine(0);
        var a = engine.ExecuteWithRngSingleNumeric("dF", new QueueRng(1));
        var b = engine.ExecuteWithRngSingleNumeric("1dF", new QueueRng(1));
        Assert.Equal(b.Total, a.Total);
        Assert.Equal(b.Rolls, a.Rolls);
    }

    [Fact]
    public void Four_dF_keep_highest_3_matches_custom_fudge_die_under_same_rng()
    {
        var engine = new DiceEngine(0);
        var a = engine.ExecuteWithRngSingleNumeric("4dFkh3", new QueueRng(0, 1, 2, 2));
        var b = engine.ExecuteWithRngSingleNumeric("4d{-1,0,1}kh3", new QueueRng(0, 1, 2, 2));
        Assert.Equal([-1, 0, 1, 1], a.Rolls);
        Assert.Equal(2, a.Total);
        Assert.Equal(b.Total, a.Total);
        Assert.Equal(b.Rolls, a.Rolls);
    }

    [Fact]
    public void Four_dF_keep_lowest_2_matches_custom_fudge_die_under_same_rng()
    {
        var engine = new DiceEngine(0);
        var a = engine.ExecuteWithRngSingleNumeric("4dFkl2", new QueueRng(2, 2, 0, 1));
        var b = engine.ExecuteWithRngSingleNumeric("4d{-1,0,1}kl2", new QueueRng(2, 2, 0, 1));
        Assert.Equal([1, 1, -1, 0], a.Rolls);
        Assert.Equal(-1, a.Total);
        Assert.Equal(b.Total, a.Total);
        Assert.Equal(b.Rolls, a.Rolls);
    }

    [Fact]
    public void Four_dFF_is_rejected()
    {
        var engine = new DiceEngine(0);
        Assert.Throws<ParseException>(() => _ = engine.Execute("4dFF"));
    }

    [Fact]
    public void Four_dF_braces_is_rejected()
    {
        var engine = new DiceEngine(0);
        Assert.Throws<ParseException>(() => _ = engine.Execute("4dF{}"));
    }

    [Fact]
    public void Four_dF_trailing_digit_is_rejected()
    {
        var engine = new DiceEngine(0);
        Assert.Throws<ParseException>(() => _ = engine.Execute("4dF2"));
    }
}
