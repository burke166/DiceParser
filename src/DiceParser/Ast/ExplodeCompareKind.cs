namespace DiceParser.Ast;

internal enum ExplodeCompareKind : byte
{
    /// <summary>Explode when the roll equals the maximum face on the die.</summary>
    EqualMax = 0,
    EqualN,
    GreaterOrEqualN,
    LessOrEqualN,
}
