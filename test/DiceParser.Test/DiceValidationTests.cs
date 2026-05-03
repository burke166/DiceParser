using DiceParser;
using DiceParser.Exceptions;

namespace DiceParser.Test;

/// <summary>Invalid inputs, parse errors, and limit guards.</summary>
public class DiceValidationTests
{
    [Fact]
    public void Negative_dice_count_throws()
    {
        // Arrange
        var engine = new DiceEngine(1);

        // Act & Assert
        var ex = Assert.Throws<EvalException>(() => _ = engine.Execute("(-1)d6"));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Zero_sides_throws()
    {
        // Arrange
        var engine = new DiceEngine(1);

        // Act & Assert
        var ex = Assert.Throws<EvalException>(() => _ = engine.Execute("3d0"));
        Assert.Contains("positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Negative_sides_throws()
    {
        // Arrange
        var engine = new DiceEngine(1);

        // Act & Assert
        var ex = Assert.Throws<EvalException>(() => _ = engine.Execute("2d-3"));
        Assert.Contains("positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Empty_custom_die_face_list_throws_at_parse()
    {
        // Arrange
        var engine = new DiceEngine(1);

        // Act & Assert
        var ex = Assert.Throws<ParseException>(() => _ = engine.Execute("1d{}"));
        Assert.Contains("at least one face", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Division_by_zero_in_operand_throws()
    {
        // Arrange
        var engine = new DiceEngine(1);

        // Act & Assert
        Assert.Throws<DivideByZeroException>(() => _ = engine.Execute("(1+1)d(1/0)"));
    }

    [Fact]
    public void Exceeding_MaxDicePerExpr_throws()
    {
        // Arrange
        var tight = Limits.Default with { MaxDicePerExpr = 5 };
        var engine = new DiceEngine(1);

        // Act & Assert
        var ex = Assert.Throws<EvalException>(() => _ = engine.Execute("6d6", tight));
        Assert.Contains("Too many dice", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Exceeding_MaxSides_throws()
    {
        // Arrange
        var tight = Limits.Default with { MaxSides = 8 };
        var engine = new DiceEngine(1);

        // Act & Assert
        var ex = Assert.Throws<EvalException>(() => _ = engine.Execute("1d10", tight));
        Assert.Contains("too large", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Malformed_program_throws_parse_exception()
    {
        // Arrange
        var engine = new DiceEngine(1);

        // Act & Assert
        Assert.Throws<ParseException>(() => _ = engine.Execute("1d6+"));
    }

    [Fact]
    public void Unclosed_parenthesis_in_operand_throws()
    {
        // Arrange
        var engine = new DiceEngine(1);

        // Act & Assert — missing ')' on the sides operand
        Assert.Throws<ParseException>(() => _ = engine.Execute("(1+2)d(6"));
    }
}
