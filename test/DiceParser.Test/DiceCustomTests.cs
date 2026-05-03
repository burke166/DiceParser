using DiceParser;

namespace DiceParser.Test;

/// <summary>Custom dice Xd&#123;A,B,C&#125; and multi-roll behavior.</summary>
public class DiceCustomTests
{
    private const int FixedSeed = 0xC0570;

    [Fact]
    public void One_custom_die_roll_is_one_recorded_face()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);
        int[] allowed = [1, 2, 3];

        // Act
        var result = engine.ExecuteSingleNumericRoll("1d{1,2,3}");

        // Assert
        Assert.Single(result.Rolls);
        Assert.Contains(result.Rolls[0], allowed);
        Assert.Equal(result.Rolls[0], result.Total);
        Assert.Equal(1, result.DiceRolled);
    }

    [Fact]
    public void Four_custom_dice_negative_zero_positive_faces_sum_matches_rolls()
    {
        // Arrange
        var engine = new DiceEngine(FixedSeed);
        int[] allowed = [-1, 0, 1];

        // Act
        var result = engine.ExecuteSingleNumericRoll("4d{-1,0,1}");

        // Assert
        Assert.Equal(4, result.Rolls.Length);
        Assert.Equal(4, result.DiceRolled);
        Assert.All(result.Rolls, v => Assert.Contains(v, allowed));
        Assert.Equal(result.Rolls.Sum(), result.Total);
    }

    [Fact]
    public void Many_custom_rolls_each_in_face_set()
    {
        // Arrange
        var engine = new DiceEngine(99);
        int[] faces = [2, 4, 6, 8];

        // Act
        var result = engine.ExecuteSingleNumericRoll("12d{2,4,6,8}");

        // Assert
        Assert.Equal(12, result.Rolls.Length);
        Assert.All(result.Rolls, v => Assert.Contains(v, faces));
        Assert.Equal(result.Rolls.Sum(), result.Total);
    }
}
