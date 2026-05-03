namespace DiceParser.Ast;

/// <summary>Reroll modifier (<c>r</c> / <c>ro</c> + optional compare + threshold).</summary>
internal readonly struct RerollSpec
{
    public readonly bool HasReroll;
    /// <summary><see langword="true"/> for <c>ro</c> (single replacement), <see langword="false"/> for <c>r</c> (until condition false).</summary>
    public readonly bool Once;
    public readonly RerollCompareKind Compare;
    public readonly int N;

    public RerollSpec(bool once, RerollCompareKind compare, int n)
    {
        HasReroll = true;
        Once = once;
        Compare = compare;
        N = n;
    }
}
