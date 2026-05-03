namespace DiceParser.Ast;

/// <summary>Threshold comparison for <c>r</c> / <c>ro</c>. <c>&lt;</c> means less than or equal to <c>N</c>; <c>&gt;</c> means greater than or equal to <c>N</c>.</summary>
internal enum RerollCompareKind : byte
{
    Equal,
    LessOrEqual,
    GreaterOrEqual,
}
