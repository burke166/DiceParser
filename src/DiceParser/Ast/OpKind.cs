namespace DiceParser.Ast;

internal enum OpKind : byte
{
    Add, Sub, Mul, Div, Mod,
    Pos, Neg, // unary
}