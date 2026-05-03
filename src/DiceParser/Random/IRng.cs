namespace DiceParser.Random;

internal interface IRng
{
    int NextInt(int minInclusive, int maxExclusive);
}
