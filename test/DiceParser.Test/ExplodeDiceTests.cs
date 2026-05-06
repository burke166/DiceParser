using DiceParser;
using DiceParser.Exceptions;

namespace DiceParser.Test;

public class ExplodeDiceTests
{
    [Fact]
    public void Three_d6_standard_explode_matches_roll20_example()
    {
        var engine = new DiceEngine(0);
        // d6 faces 1..6; indices 2,3,5 -> 3,4,6 then 6,2
        var r = engine.ExecuteWithRngSingleNumeric("3d6!", new QueueRng(2, 3, 5, 5, 1));

        Assert.Equal([3, 4, 6, 6, 2], r.Rolls);
        Assert.Equal(21, r.Total);
        Assert.Equal(5, r.DiceRolled);
    }

    [Fact]
    public void Four_d6_compound_explode_matches_roll20_example()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("4d6!!", new QueueRng(2, 3, 4, 5, 5, 2));

        Assert.Equal([3, 4, 5, 15], r.Rolls);
        Assert.Equal(27, r.Total);
        Assert.Equal(6, r.DiceRolled);
    }

    [Fact]
    public void Three_d6_penetrating_explode_matches_roll20_example()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("3d6!p", new QueueRng(2, 3, 5, 5, 1));

        Assert.Equal([3, 4, 6, 5, 1], r.Rolls);
        Assert.Equal(19, r.Total);
        Assert.Equal(5, r.DiceRolled);
    }

    [Fact]
    public void One_d6_compound_penetrating_collapses_chain_using_penetrating_addends()
    {
        var engine = new DiceEngine(0);
        // Raw chain 6, 6, 2 (same as third die in 3d6!p): first counts full, then +5 and +1 -> single compounded 12
        var r = engine.ExecuteWithRngSingleNumeric("1d6!!p", new QueueRng(5, 5, 1));

        Assert.Single(r.Rolls);
        Assert.Equal(12, r.Rolls[0]);
        Assert.Equal(12, r.Total);
        Assert.Equal(3, r.DiceRolled);
    }

    [Fact]
    public void Compound_penetrating_accepts_compare_suffix()
    {
        var engine = new DiceEngine(0);
        // Raw 6 then 3 with !>4: sum = 6 + (3 - 1) = 8
        var r = engine.ExecuteWithRngSingleNumeric("1d6!!p>4", new QueueRng(5, 2));

        Assert.Single(r.Rolls);
        Assert.Equal(8, r.Total);
        Assert.Equal(2, r.DiceRolled);
    }

    [Fact]
    public void Three_d6_explode_on_exact_5_matches_roll20_example()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("3d6!5", new QueueRng(2, 3, 4, 4, 1));

        Assert.Equal([3, 4, 5, 5, 2], r.Rolls);
        Assert.Equal(19, r.Total);
        Assert.Equal(5, r.DiceRolled);
    }

    [Fact]
    public void Three_d6_explode_greater_or_equal_4_matches_roll20_example()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("3d6!>4", new QueueRng(2, 3, 1, 4, 4, 1));

        Assert.Equal([3, 4, 2, 5, 5, 2], r.Rolls);
        Assert.Equal(21, r.Total);
        Assert.Equal(6, r.DiceRolled);
    }

    [Fact]
    public void Four_d6_compound_explode_greater_or_equal_4_matches_roll20_example()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("4d6!!>4", new QueueRng(2, 3, 2, 4, 2, 5, 5, 2));

        Assert.Equal([3, 7, 8, 15], r.Rolls);
        Assert.Equal(33, r.Total);
        Assert.Equal(8, r.DiceRolled);
    }

    [Fact]
    public void One_d6_penetrating_explode_greater_or_equal_4()
    {
        var engine = new DiceEngine(0);
        // Raw 4 (kept), raw 5 -> +4, raw 2 -> +1; explode checks use raw values
        var r = engine.ExecuteWithRngSingleNumeric("1d6!p>4", new QueueRng(3, 4, 1));

        Assert.Equal([4, 4, 1], r.Rolls);
        Assert.Equal(9, r.Total);
        Assert.Equal(3, r.DiceRolled);
    }

    [Fact]
    public void Plain_dice_and_arithmetic_still_work()
    {
        var engine = new DiceEngine(42);
        var r = engine.ExecuteSingleNumericRoll("3d10+4");
        Assert.Equal(3, r.Rolls.Length);
        Assert.Equal(r.Rolls.Sum() + 4, r.Total);
    }

    [Fact]
    public void Keep_highest_still_works_after_explode_standard_expands_pool()
    {
        var engine = new DiceEngine(0);
        // 2d4!: first die 4+4+1, second die 1 -> kh1 keeps a single highest 4 (stable tie on first 4)
        var r = engine.ExecuteWithRngSingleNumeric("2d4!kh1", new QueueRng(3, 3, 0, 0));

        Assert.Equal([4, 4, 1, 1], r.Rolls);
        Assert.Equal(4, r.Total);
        Assert.Equal(4, r.DiceRolled);
    }

    [Fact]
    public void Keep_highest_on_compound_explode_uses_one_slot_per_original_die()
    {
        var engine = new DiceEngine(0);
        // Two d10, max face 10. First die 10+10+3=23, second die 2. kh1 -> 23
        var r = engine.ExecuteWithRngSingleNumeric("2d10!!kh1", new QueueRng(9, 9, 2, 1));

        Assert.Equal([23, 2], r.Rolls);
        Assert.Equal(23, r.Total);
    }

    [Fact]
    public void Explode_compare_out_of_range_throws()
    {
        var engine = new DiceEngine(0);
        var ex = Assert.Throws<EvalException>(() =>
            _ = engine.ExecuteWithRng("2d6!7", new QueueRng(0, 0)));
        Assert.Contains("between", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Custom_die_explode_compare_validated_against_face_min_max()
    {
        var engine = new DiceEngine(0);
        var ex = Assert.Throws<EvalException>(() =>
            _ = engine.ExecuteWithRng("1d{2,4,6,8}!9", new QueueRng(0)));
        Assert.Contains("between", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Custom_die_explode_compare_below_face_min_throws()
    {
        var engine = new DiceEngine(0);
        var ex = Assert.Throws<EvalException>(() =>
            _ = engine.ExecuteWithRng("1d{2,4,6,8}!1", new QueueRng(0)));
        Assert.Contains("between", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Custom_die_explode_on_max_face_works()
    {
        var engine = new DiceEngine(0);
        var r = engine.ExecuteWithRngSingleNumeric("2d{2,4,6,8}!", new QueueRng(3, 1, 0));

        Assert.Equal([8, 4, 2], r.Rolls);
        Assert.Equal(14, r.Total);
    }

    [Fact]
    public void Custom_die_explode_less_or_equal_uses_face_values()
    {
        var engine = new DiceEngine(0);
        // First roll is 2 (<=4), so it explodes once into a 6.
        var r = engine.ExecuteWithRngSingleNumeric("1d{2,4,6,8}!<4", new QueueRng(0, 2));

        Assert.Equal([2, 6], r.Rolls);
        Assert.Equal(8, r.Total);
        Assert.Equal(2, r.DiceRolled);
    }

    [Fact]
    public void Explosion_chain_respects_MaxDicePerExpr()
    {
        var tight = Limits.Default with { MaxDicePerExpr = 4 };
        var engine = new DiceEngine(0);
        // d4 always explodes on max: 4,4,4,... — fifth roll must throw
        var ex = Assert.Throws<EvalException>(() =>
            _ = engine.ExecuteWithRng("1d4!", new QueueRng(3, 3, 3, 3), tight));
        Assert.Contains("Too many dice", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
