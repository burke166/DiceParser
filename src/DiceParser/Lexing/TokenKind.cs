namespace DiceParser.Lexing;

internal enum TokenKind : byte
{
    End,
    Number,

    Plus, Minus, Star, Slash, Percent,
    LParen, RParen,
    LBrace, RBrace, // '{' or '}'
    Comma,
    Semicolon,

    Greater, // '>' (explode compare)
    Less,    // '<' (explode compare)

    ExplodeStandard,     // '!'
    ExplodeCompound,     // '!!'
    ExplodePenetrating,   // '!p' / '!P'
    ExplodeCompoundPenetrating, // '!!p' / '!!P'

    D,          // 'd' or 'D'
    Identifier, // e.g. 'kh', 'kl'
}
