namespace DiceParser.Ast;

internal enum DiceModKind : byte
{
    None = 0,
    KeepHighest,
    KeepLowest,
    DropHighest,
    DropLowest,
}
