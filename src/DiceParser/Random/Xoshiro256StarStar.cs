namespace DiceParser.Random;

internal struct Xoshiro256StarStar
{
    private ulong _s0, _s1, _s2, _s3;

    public Xoshiro256StarStar(ulong seed)
    {
        // SplitMix64 to expand seed
        ulong x = seed;
        _s0 = SplitMix64(ref x);
        _s1 = SplitMix64(ref x);
        _s2 = SplitMix64(ref x);
        _s3 = SplitMix64(ref x);

        // avoid all-zero state
        if ((_s0 | _s1 | _s2 | _s3) == 0)
            _s0 = 0x9E3779B97F4A7C15UL;
    }

    private static ulong SplitMix64(ref ulong x)
    {
        ulong z = (x += 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

    public ulong NextU64()
    {
        ulong result = Rotl(_s1 * 5, 7) * 9;

        ulong t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;

        _s2 ^= t;
        _s3 = Rotl(_s3, 45);

        return result;
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive) throw new ArgumentOutOfRangeException();
        uint range = (uint)(maxExclusive - minInclusive);

        // rejection sampling for uniformity
        uint limit = uint.MaxValue - (uint.MaxValue % range);
        uint r;
        do
        {
            r = (uint)NextU64();
        } while (r >= limit);

        return (int)(minInclusive + (r % range));
    }
}
