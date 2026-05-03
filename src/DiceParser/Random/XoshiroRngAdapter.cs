namespace DiceParser.Random;

internal sealed class XoshiroRngAdapter : IRng
{
    public Xoshiro256StarStar State;

    public XoshiroRngAdapter(Xoshiro256StarStar state) => State = state;

    public int NextInt(int minInclusive, int maxExclusive) => State.NextInt(minInclusive, maxExclusive);
}
