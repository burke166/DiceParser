using DiceParser;
using DiceParser.Random;
using Xunit;

public sealed class CryptoDiceRandomTests
{
    [Fact]
    public void NextInt_ReturnsValueWithinRange()
    {
        var rng = new CryptoDiceRandom();

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.NextInt(1, 7);

            Assert.InRange(value, 1, 6);
        }
    }

    [Fact]
    public void NextInt_WithSinglePossibleValue_ReturnsThatValue()
    {
        var rng = new CryptoDiceRandom();

        for (int i = 0; i < 1_000; i++)
        {
            int value = rng.NextInt(5, 6);

            Assert.Equal(5, value);
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(10, -10)]
    public void NextInt_WithInvalidRange_Throws(int minInclusive, int maxExclusive)
    {
        var rng = new CryptoDiceRandom();

        Assert.Throws<ArgumentException>(() =>
            rng.NextInt(minInclusive, maxExclusive));
    }

    [Fact]
    public void DiceEngine_CreateCrypto_executes_without_throwing()
    {
        var engine = DiceEngine.CreateCrypto();
        var results = engine.Execute("1d6");
        Assert.Single(results);
    }
}