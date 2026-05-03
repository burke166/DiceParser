using DiceParser;

namespace DiceParser.Test;

/// <summary>Standard dice XdY: ranges, sums, zero dice, and arithmetic tails (XdY+Z).</summary>
public class DiceStandardTests
{
    private const int FixedSeed = 0x5EED;

    [Fact]
    public void One_d6_total_is_in_valid_face_range()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);

        // Act
        var result = engine.Execute("1d6").Single();

        // Assert
        Assert.InRange(result.Total, 1, 6);
        Assert.Single(result.Rolls);
        Assert.Equal(result.Total, result.Rolls[0]);
        Assert.Equal(1, result.DiceRolled);
    }

    [Fact]
    public void Three_d6_total_equals_sum_of_recorded_rolls()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);

        // Act
        var result = engine.Execute("3d6").Single();

        // Assert
        Assert.Equal(3, result.Rolls.Length);
        Assert.Equal(3, result.DiceRolled);
        Assert.Equal(result.Rolls.Sum(), result.Total);
        Assert.All(result.Rolls, v => Assert.InRange(v, 1, 6));
    }

    [Fact]
    public void Zero_d6_returns_zero_without_rolls()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);

        // Act
        var result = engine.Execute("0d6").Single();

        // Assert
        Assert.Equal(0, result.Total);
        Assert.Empty(result.Rolls);
        Assert.Equal(0, result.DiceRolled);
    }

    [Fact]
    public void Two_d6_plus_constant_adds_only_to_total()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);

        // Act
        var result = engine.Execute("2d6+3").Single();

        // Assert
        Assert.Equal(2, result.Rolls.Length);
        Assert.Equal(2, result.DiceRolled);
        var diceSum = result.Rolls.Sum();
        Assert.Equal(diceSum + 3, result.Total);
    }

    [Fact]
    public void Implied_count_d6_matches_one_d_six_with_same_seed()
    {
        // Arrange
        var a = new DiceEngine(FixedSeed);
        var b = new DiceEngine(FixedSeed);

        // Act
        var implied = a.Execute("d6").Single();
        var explicitOne = b.Execute("1d6").Single();

        // Assert
        Assert.Equal(explicitOne.Total, implied.Total);
        Assert.Equal(explicitOne.Rolls, implied.Rolls);
    }

    [Fact]
    public void Same_seed_same_expression_is_deterministic()
    {
        // Arrange
        var a = new DiceEngine(4242);
        var b = new DiceEngine(4242);

        // Act
        var ra = a.Execute("5d10").Single();
        var rb = b.Execute("5d10").Single();

        // Assert
        Assert.Equal(ra.Total, rb.Total);
        Assert.Equal(ra.Rolls, rb.Rolls);
    }
}
