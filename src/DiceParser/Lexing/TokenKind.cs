namespace DiceParser.Lexing;

internal enum TokenKind : byte
{
    End,
    Number,

    Plus, Minus, Star, Slash, Percent,
    LParen, RParen,
    Semicolon,

    D,          // 'd' or 'D'
    Identifier, // for future modifiers, e.g., 'kh', 'ro', etc.
}