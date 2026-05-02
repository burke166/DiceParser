namespace DiceParser.Ast;

internal enum NodeKind : byte
{
    Number,
    Unary,
    Binary,
    Dice,
}