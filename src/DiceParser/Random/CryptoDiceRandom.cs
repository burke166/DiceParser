using System.Security.Cryptography;

namespace DiceParser.Random;

/// <summary><see cref="IDiceRandom"/> implementation using <see cref="System.Security.Cryptography.RandomNumberGenerator"/>.</summary>
public sealed class CryptoDiceRandom : IDiceRandom
{
    /// <summary>Returns a uniformly distributed random 32-bit integer in <paramref name="minInclusive"/>..<paramref name="maxExclusive"/>.</summary>
    /// <param name="minInclusive">Inclusive lower bound.</param>
    /// <param name="maxExclusive">Exclusive upper bound.</param>
    public int NextInt(int minInclusive, int maxExclusive)
        => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
}