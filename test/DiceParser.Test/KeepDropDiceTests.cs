using DiceParser;
using DiceParser.Exceptions;

namespace DiceParser.Test;

public class KeepDropDiceTests
{
    private static int SumKeepHighestStable(int[] rolls, int n)
    {
        var order = new int[rolls.Length];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;

        Array.Sort(order, (a, b) =>
        {
            int c = rolls[b].CompareTo(rolls[a]);
            return c != 0 ? c : a.CompareTo(b);
        });

        int sum = 0;
        for (int i = 0; i < n; i++)
            sum += rolls[order[i]];
        return sum;
    }

    private static int SumKeepLowestStable(int[] rolls, int n)
    {
        var order = new int[rolls.Length];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;

        Array.Sort(order, (a, b) =>
        {
            int c = rolls[a].CompareTo(rolls[b]);
            return c != 0 ? c : a.CompareTo(b);
        });

        int sum = 0;
        for (int i = 0; i < n; i++)
            sum += rolls[order[i]];
        return sum;
    }

    private static int SumDropHighestStable(int[] rolls, int drop)
    {
        var order = new int[rolls.Length];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;

        Array.Sort(order, (a, b) =>
        {
            int c = rolls[b].CompareTo(rolls[a]);
            return c != 0 ? c : a.CompareTo(b);
        });

        int sum = 0;
        for (int i = drop; i < order.Length; i++)
            sum += rolls[order[i]];
        return sum;
    }

    private static int SumDropLowestStable(int[] rolls, int drop)
    {
        var order = new int[rolls.Length];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;

        Array.Sort(order, (a, b) =>
        {
            int c = rolls[a].CompareTo(rolls[b]);
            return c != 0 ? c : a.CompareTo(b);
        });

        int sum = 0;
        for (int i = drop; i < order.Length; i++)
            sum += rolls[order[i]];
        return sum;
    }

    [Fact]
    public void Four_d6_keep_highest_3_matches_stable_ranking()
    {
        var engine = new DiceEngine(2026);
        var r = engine.ExecuteSingleNumericRoll("4d6kh3");
        Assert.Equal(4, r.Rolls.Length);
        Assert.Equal(SumKeepHighestStable(r.Rolls, 3), r.Total);
    }

    [Fact]
    public void Four_d6_keep_lowest_3_matches_stable_ranking()
    {
        var engine = new DiceEngine(2026);
        var r = engine.ExecuteSingleNumericRoll("4d6kl3");
        Assert.Equal(4, r.Rolls.Length);
        Assert.Equal(SumKeepLowestStable(r.Rolls, 3), r.Total);
    }

    [Fact]
    public void Four_d6_drop_highest_1_matches_stable_ranking()
    {
        var engine = new DiceEngine(2026);
        var r = engine.ExecuteSingleNumericRoll("4d6dh1");
        Assert.Equal(4, r.Rolls.Length);
        Assert.Equal(SumDropHighestStable(r.Rolls, 1), r.Total);
    }

    [Fact]
    public void Four_d6_drop_lowest_1_matches_stable_ranking()
    {
        var engine = new DiceEngine(2026);
        var r = engine.ExecuteSingleNumericRoll("4d6dl1");
        Assert.Equal(4, r.Rolls.Length);
        Assert.Equal(SumDropLowestStable(r.Rolls, 1), r.Total);
    }

    [Fact]
    public void Two_d20_keep_highest_1_advantage_style()
    {
        var engine = new DiceEngine(99);
        var r = engine.ExecuteSingleNumericRoll("2d20kh1");
        Assert.Equal(2, r.Rolls.Length);
        Assert.Equal(SumKeepHighestStable(r.Rolls, 1), r.Total);
        Assert.Equal(Math.Max(r.Rolls[0], r.Rolls[1]), r.Total);
    }

    [Fact]
    public void Two_d20_keep_lowest_1_disadvantage_style()
    {
        var engine = new DiceEngine(99);
        var r = engine.ExecuteSingleNumericRoll("2d20kl1");
        Assert.Equal(2, r.Rolls.Length);
        Assert.Equal(SumKeepLowestStable(r.Rolls, 1), r.Total);
        Assert.Equal(Math.Min(r.Rolls[0], r.Rolls[1]), r.Total);
    }

    [Fact]
    public void Keep_drop_modifier_case_insensitive()
    {
        var engine = new DiceEngine(1);
        var lower = engine.ExecuteSingleNumericRoll("3d6kh2");
        var engine2 = new DiceEngine(1);
        var upper = engine2.ExecuteSingleNumericRoll("3d6KH2");
        Assert.Equal(lower.Rolls, upper.Rolls);
        Assert.Equal(lower.Total, upper.Total);
    }

    [Fact]
    public void Invalid_keep_count_zero_is_parse_error()
    {
        var engine = new DiceEngine(1);
        var ex = Assert.Throws<ParseException>(() => engine.Execute("4d6kh0"));
        Assert.Contains("positive integer", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Invalid_keep_count_exceeds_dice_count_is_eval_error()
    {
        var engine = new DiceEngine(1);
        var ex = Assert.Throws<EvalException>(() => engine.Execute("1d6kh2"));
        Assert.Contains("cannot exceed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Regression_plain_dice_plus_constant_unchanged()
    {
        var engine = new DiceEngine(42);
        var withModEngine = new DiceEngine(42);
        var plain = engine.ExecuteSingleNumericRoll("3d10+4");
        var withMod = withModEngine.ExecuteSingleNumericRoll("3d10+4");
        Assert.Equal(plain.Rolls, withMod.Rolls);
        Assert.Equal(plain.Total, withMod.Total);
        Assert.Equal(plain.DiceRolled, withMod.DiceRolled);
    }

    [Fact]
    public void Custom_die_faces_use_same_keep_drop_logic()
    {
        var engine = new DiceEngine(7);
        var r = engine.ExecuteSingleNumericRoll("5d{2,4,6,8,10}kh3");
        Assert.Equal(5, r.Rolls.Length);
        Assert.Equal(SumKeepHighestStable(r.Rolls, 3), r.Total);
        foreach (var v in r.Rolls)
            Assert.Contains(v, new[] { 2, 4, 6, 8, 10 });
    }
}
