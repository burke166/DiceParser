using DiceParser;

namespace DiceParser.Test;

/// <summary>Deterministic queue-backed RNG; <see cref="IRng.NextInt"/> must return face indices in [min, max).</summary>
internal sealed class QueueRng : IDiceRandom
{
    private readonly int[] _values;
    private int _i;

    public QueueRng(params int[] values) => _values = values;

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (_i >= _values.Length)
            throw new InvalidOperationException("RNG queue exhausted.");

        int v = _values[_i++];
        if (v < minInclusive || v >= maxExclusive)
            throw new InvalidOperationException($"Dequeued index {v} not in [{minInclusive},{maxExclusive}).");

        return v;
    }
}