namespace DiceParser.Lexing;

internal readonly struct Token
{
    public readonly TokenKind Kind;
    public readonly int IntValue;     // valid if Kind == Number
    public readonly int Start;        // for Identifier slices
    public readonly int Length;       // for Identifier slices

    public Token(TokenKind kind, int intValue = 0, int start = 0, int length = 0)
    {
        Kind = kind;
        IntValue = intValue;
        Start = start;
        Length = length;
    }

    public override string ToString()
        => Kind == TokenKind.Number ? $"Number({IntValue})" : Kind.ToString();
}