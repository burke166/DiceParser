namespace DiceParser.Ast;

internal readonly struct DiceMod
{
    public readonly DiceModKind Kind;
    public readonly int N;

    public DiceMod(DiceModKind kind, int n)
    {
        Kind = kind;
        N = n;
    }
}
