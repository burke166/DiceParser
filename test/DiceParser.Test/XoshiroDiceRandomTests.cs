using DiceParser.Random;
using Xunit;

namespace DiceParser.Tests;

public sealed class XoshiroDiceRandomTests
{
    [Fact]
    public void NextInt_ReturnsValueWithinRange()
    {
        var rng = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 12345));

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.NextInt(1, 7);

            Assert.InRange(value, 1, 6);
        }
    }

    [Fact]
    public void NextInt_WithSinglePossibleValue_ReturnsThatValue()
    {
        var rng = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 12345));

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
        var rng = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 12345));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            rng.NextInt(minInclusive, maxExclusive));
    }

    [Fact]
    public void NextInt_WithSameSeed_ProducesSameSequence()
    {
        var rng1 = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 12345));
        var rng2 = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 12345));

        for (int i = 0; i < 1_000; i++)
        {
            Assert.Equal(
                rng1.NextInt(1, 101),
                rng2.NextInt(1, 101));
        }
    }

    [Fact]
    public void NextInt_WithDifferentSeeds_ProducesDifferentSequence()
    {
        var rng1 = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 12345));
        var rng2 = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 54321));

        bool foundDifference = false;

        for (int i = 0; i < 100; i++)
        {
            if (rng1.NextInt(1, 101) != rng2.NextInt(1, 101))
            {
                foundDifference = true;
                break;
            }
        }

        Assert.True(foundDifference);
    }

    [Fact]
    public void NextInt_AdvancesState()
    {
        var rng = new XoshiroDiceRandom(new Xoshiro256StarStar(seed: 12345));

        int first = rng.NextInt(1, int.MaxValue);
        int second = rng.NextInt(1, int.MaxValue);

        Assert.NotEqual(first, second);
    }
}