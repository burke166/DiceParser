using DiceParser;
using DiceParser.Exceptions;

namespace DiceParser.Test;

public class RerollDiceTests
{
    [Fact]
    public void Four_d6_r1_never_ends_with_a_1()
    {
        var engine = new DiceEngine(0);
        // Each die: roll 1 (index 0) then reroll to 6 (index 5)
        var r = engine.ExecuteWithRngSingleNumeric("4d6r1", new QueueRng(0, 5, 0, 5, 0, 5, 0, 5));

        Assert.Equal([6, 6, 6, 6], r.Rolls);
        Assert.DoesNotContain(1, r.Rolls);
        Assert.Equal(8, r.DiceRolled);
    }

    [Fact]
    public void Four_d6_ro_less_than_3_each_die_rolls_at_most_twice()
    {
        var engine = new DiceEngine(0);
        // r<3 means ≤3: first outcome 2 → one reroll each; second outcome 1 is kept (once only)
        var r = engine.ExecuteWithRngSingleNumeric("4d6ro<3", new QueueRng(1, 0, 1, 0, 1, 0, 1, 0));

        Assert.Equal([1, 1, 1, 1], r.Rolls);
        Assert.Equal(8, r.DiceRolled);
    }

    [Fact]
    public void Four_d6_r_less_than_or_equal_3_all_final_faces_above_3()
    {
        var engine = new DiceEngine(0);
        // r<3 means ≤3: per die 1, 1, 3 still match, then 4 stops (indices 0, 0, 2, 3)
        var r = engine.ExecuteWithRngSingleNumeric(
            "4d6r<3",
            new QueueRng(0, 0, 2, 3, 0, 0, 2, 3, 0, 0, 2, 3, 0, 0, 2, 3));

        Assert.Equal(4, r.Rolls.Length);
        Assert.All(r.Rolls, v => Assert.True(v > 3));
        Assert.Equal(16, r.DiceRolled);
    }

    [Fact]
    public void Continuous_r_less_than_N_rejected_when_N_not_below_highest_face()
    {
        var engine = new DiceEngine(0);

        var ex6 = Assert.Throws<EvalException>(() => _ = engine.Execute("1d6r<6"));
        Assert.Contains("highest face value 6", ex6.Message, StringComparison.OrdinalIgnoreCase);

        var ex7 = Assert.Throws<EvalException>(() => _ = engine.Execute("1d6r<7"));
        Assert.Contains("highest face value 6", ex7.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reroll_once_ro_less_than_high_threshold_still_allowed_on_d6()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("1d6ro<7", new QueueRng(0, 5));

        Assert.Single(r.Rolls);
        Assert.Equal(6, r.Rolls[0]);
        Assert.Equal(2, r.DiceRolled);
    }

    [Fact]
    public void Reroll_applies_before_standard_explode_on_each_physical_roll()
    {
        var engine = new DiceEngine(0);
        // 1 then reroll to 6; explode on 6; next roll 6 explodes; then 2 stops
        var r = engine.ExecuteWithRngSingleNumeric("1d6r1!", new QueueRng(0, 5, 5, 1));

        Assert.Equal([6, 6, 2], r.Rolls);
        Assert.Equal(14, r.Total);
        Assert.Equal(4, r.DiceRolled);
    }

    [Fact]
    public void Reroll_may_follow_explode_in_source_text()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("1d6!r1", new QueueRng(0, 5, 5, 1));

        Assert.Equal([6, 6, 2], r.Rolls);
        Assert.Equal(4, r.DiceRolled);
    }

    [Fact]
    public void Duplicate_reroll_modifier_throws_parse()
    {
        var engine = new DiceEngine(0);
        Assert.Throws<ParseException>(() => _ = engine.Execute("1d6r1r1"));
    }

    [Fact]
    public void Reroll_with_explicit_equals_form()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("2d6r=6", new QueueRng(5, 0, 5, 0));

        Assert.Equal([1, 1], r.Rolls);
        Assert.Equal(4, r.DiceRolled);
    }

    [Fact]
    public void Continuous_reroll_has_iteration_safety_guard()
    {
        var engine = new DiceEngine(0);
        var alwaysOne = Enumerable.Repeat(0, 200).ToArray();

        var ex = Assert.Throws<EvalException>(() =>
            _ = engine.ExecuteWithRngSingleNumeric("1d6r1", new QueueRng(alwaysOne)));

        Assert.Contains("Reroll limit exceeded", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
