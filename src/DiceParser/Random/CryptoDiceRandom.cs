using System.Security.Cryptography;

namespace DiceParser.Random;

public sealed class CryptoDiceRandom : IDiceRandom
{
    public int NextInt(int minInclusive, int maxExclusive)
        => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
}