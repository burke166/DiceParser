using DiceParser;

namespace DiceParser.Test;

/// <summary>Operand expressions, programs with semicolons, and evaluation vs equivalent forms.</summary>
public class DiceExpressionTests
{
    private const int FixedSeed = 0xC0FFEE;

    [Fact]
    public void Parenthesized_count_and_sides_behaves_like_plain_3d6()
    {
        // Arrange
        var plain = new DiceEngine(FixedSeed);
        var expr = new DiceEngine(FixedSeed);

        // Act
        var a = plain.ExecuteSingleNumericRoll("3d6");
        var b = expr.ExecuteSingleNumericRoll("(1+2)d(3+3)");

        // Assert — same dice count and sides → same RNG draws in the same order
        Assert.Equal(a.Total, b.Total);
        Assert.Equal(a.Rolls, b.Rolls);
        Assert.Equal(a.DiceRolled, b.DiceRolled);
    }

    [Fact]
    public void Nested_arithmetic_in_operands_before_roll()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);
        var reference = new DiceEngine(FixedSeed);

        // Act — (1+1)=2, (2*3)=6 → 2d6
        var actual = engine.ExecuteSingleNumericRoll("(1+1)d(2*3)");
        var expected = reference.ExecuteSingleNumericRoll("2d6");

        // Assert
        Assert.Equal(expected.Total, actual.Total);
        Assert.Equal(expected.Rolls, actual.Rolls);
    }

    [Fact]
    public void Semicolon_separates_independent_expressions_with_separate_results()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);

        // Act
        var results = engine.Execute("1d6;2d6").ToList();

        // Assert — two roll results, not one combined total
        Assert.Equal(2, results.Count);
        var r0 = Assert.IsType<NumericExpressionResult>(results[0]).Roll;
        var r1 = Assert.IsType<NumericExpressionResult>(results[1]).Roll;
        Assert.Single(r0.Rolls);
        Assert.Equal(2, r1.Rolls.Length);
        Assert.Equal(r0.Rolls.Sum(), r0.Total);
        Assert.Equal(r1.Rolls.Sum(), r1.Total);
        Assert.Equal(1, r0.DiceRolled);
        Assert.Equal(2, r1.DiceRolled);
    }

    [Fact]
    public void Three_expressions_produce_three_results()
    {
        // Arrange
        var engine = new DiceEngine(7);

        // Act
        var results = engine.Execute("1d4;1d4;2d8").ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Single(Assert.IsType<NumericExpressionResult>(results[0]).Roll.Rolls);
        Assert.Single(Assert.IsType<NumericExpressionResult>(results[1]).Roll.Rolls);
        Assert.Equal(2, Assert.IsType<NumericExpressionResult>(results[2]).Roll.Rolls.Length);
    }

    [Fact]
    public void Trailing_semicolon_after_single_expression_is_accepted()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);

        // Act
        var results = engine.Execute("2d10;").ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(2, Assert.IsType<NumericExpressionResult>(results[0]).Roll.Rolls.Length);
    }
}
