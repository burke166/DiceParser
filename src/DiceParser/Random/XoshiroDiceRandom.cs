namespace DiceParser.Random;

internal sealed class XoshiroDiceRandom : IDiceRandom
{
    public Xoshiro256StarStar State;

    public XoshiroDiceRandom(Xoshiro256StarStar state) => State = state;

    public int NextInt(int minInclusive, int maxExclusive) => State.NextInt(minInclusive, maxExclusive);
}
