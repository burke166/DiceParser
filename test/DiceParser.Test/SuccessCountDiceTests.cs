using DiceParser;

namespace DiceParser.Test;

public class SuccessCountDiceTests
{
    [Fact]
    public void Four_d6_success_count_uses_final_die_values()
    {
        var engine = new DiceEngine(0);
        // d6 indices -> 4,5,6,3
        var r = engine.ExecuteWithRngSingleNumeric("4d6>=5", new QueueRng(3, 4, 5, 2));

        Assert.Equal([4, 5, 6, 3], r.Rolls);
        Assert.Equal(2, r.Total);
        Assert.Equal(4, r.DiceRolled);
    }

    [Fact]
    public void Four_d6_compound_explode_then_success_uses_one_value_per_original_die()
    {
        var engine = new DiceEngine(0);
        // Same sequence as Four_d6_compound_explode_matches_roll20_example
        var r = engine.ExecuteWithRngSingleNumeric("4d6!!>=8", new QueueRng(2, 3, 4, 5, 5, 2));

        Assert.Equal([3, 4, 5, 15], r.Rolls);
        Assert.Equal(1, r.Total);
        Assert.Equal(6, r.DiceRolled);
    }

    [Fact]
    public void Compound_explode_with_compare_then_success_counts_compounded_totals()
    {
        var engine = new DiceEngine(0);
        // !!>5: explode on roll >= 5. First die 6,6,1 -> 13 >= 8; others 1,2,3 fail threshold.
        var r = engine.ExecuteWithRngSingleNumeric("4d6!!>5>=8", new QueueRng(5, 5, 0, 0, 1, 2));

        Assert.Equal([13, 1, 2, 3], r.Rolls);
        Assert.Equal(1, r.Total);
        Assert.Equal(6, r.DiceRolled);
    }

    [Fact]
    public void Keep_highest_applies_before_success_counting()
    {
        var engine = new DiceEngine(0);
        // Six d10: values 3,9,7,10,2,8 — keep highest 3 → 10,9,8 — all >= 8 → 3 successes
        var r = engine.ExecuteWithRngSingleNumeric("6d10kh3>=8", new QueueRng(2, 8, 6, 9, 1, 7));

        Assert.Equal([3, 9, 7, 10, 2, 8], r.Rolls);
        Assert.Equal(3, r.Total);
        Assert.Equal(6, r.DiceRolled);
    }
}
