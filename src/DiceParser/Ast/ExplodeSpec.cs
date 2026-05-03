namespace DiceParser.Ast;

internal readonly struct ExplodeSpec
{
    public readonly ExplodeMode Mode;
    public readonly ExplodeCompareKind Compare;
    public readonly int N;

    public ExplodeSpec(ExplodeMode mode, ExplodeCompareKind compare, int n)
    {
        Mode = mode;
        Compare = compare;
        N = n;
    }

    public bool HasExplode => Mode != ExplodeMode.None;
}
