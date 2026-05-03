namespace DiceParser;

internal interface IDiceRandom
{
    int NextInt(int minInclusive, int maxExclusive);
}
