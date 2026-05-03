using DiceParser.Exceptions;
using System.Globalization;

namespace DiceParser.Lexing;

internal ref struct Lexer
{
    private ReadOnlySpan<char> _s;
    private int _i;

    public Lexer(ReadOnlySpan<char> s)
    {
        _s = s;
        _i = 0;
        Current = default;
        Next();
    }

    public Token Current { get; private set; }

    public void Next()
    {
        SkipWs();
        if (_i >= _s.Length)
        {
            Current = new Token(TokenKind.End);
            return;
        }

        char c = _s[_i];

        // Single-char tokens
        switch (c)
        {
            case '+': _i++; Current = new Token(TokenKind.Plus); return;
            case '-': _i++; Current = new Token(TokenKind.Minus); return;
            case '*': _i++; Current = new Token(TokenKind.Star); return;
            case '/': _i++; Current = new Token(TokenKind.Slash); return;
            case '%': _i++; Current = new Token(TokenKind.Percent); return;
            case '(': _i++; Current = new Token(TokenKind.LParen); return;
            case ')': _i++; Current = new Token(TokenKind.RParen); return;
            case ';': _i++; Current = new Token(TokenKind.Semicolon); return;
            case '{': _i++; Current = new Token(TokenKind.LBrace); return;
            case '}': _i++; Current = new Token(TokenKind.RBrace); return;
            case ',': _i++; Current = new Token(TokenKind.Comma); return;
            case '>':
                _i++;
                if (_i < _s.Length && _s[_i] == '=')
                {
                    _i++;
                    Current = new Token(TokenKind.GreaterEqual);
                    return;
                }

                Current = new Token(TokenKind.Greater);
                return;
            case '<': _i++; Current = new Token(TokenKind.Less); return;
            case '=': _i++; Current = new Token(TokenKind.Equal); return;
        }

        if (c == '!')
        {
            if (_i + 1 < _s.Length && _s[_i + 1] == '!')
            {
                _i += 2;
                if (_i < _s.Length && (_s[_i] == 'p' || _s[_i] == 'P'))
                {
                    _i++;
                    Current = new Token(TokenKind.ExplodeCompoundPenetrating);
                    return;
                }

                Current = new Token(TokenKind.ExplodeCompound);
                return;
            }

            if (_i + 1 < _s.Length && (_s[_i + 1] == 'p' || _s[_i + 1] == 'P'))
            {
                _i += 2;
                Current = new Token(TokenKind.ExplodePenetrating);
                return;
            }

            _i++;
            Current = new Token(TokenKind.ExplodeStandard);
            return;
        }

        // Dice operator — do not split two-letter keep/drop tags 'dh' / 'dl' (otherwise '4d6dh1' breaks).
        if (c == 'd' || c == 'D')
        {
            if (_i + 1 < _s.Length)
            {
                char n = _s[_i + 1];
                if (n is 'h' or 'H' or 'l' or 'L')
                {
                    int start = _i;
                    _i += 2;
                    Current = new Token(TokenKind.Identifier, start: start, length: 2);
                    return;
                }
            }

            _i++;
            Current = new Token(TokenKind.D);
            return;
        }

        // Number
        if (char.IsAsciiDigit(c))
        {
            int start = _i;
            while (_i < _s.Length && char.IsAsciiDigit(_s[_i])) _i++;
            // Parse int without allocations
            if (!int.TryParse(_s.Slice(start, _i - start), NumberStyles.None, CultureInfo.InvariantCulture, out int value))
                throw new ParseException($"Invalid number at {start}");
            Current = new Token(TokenKind.Number, value);
            return;
        }

        // Identifier (future: modifiers like kh, kl, ro, cs, etc.)
        if (char.IsAsciiLetter(c))
        {
            int start = _i;
            while (_i < _s.Length && char.IsAsciiLetter(_s[_i])) _i++;
            Current = new Token(TokenKind.Identifier, start: start, length: _i - start);
            return;
        }

        throw new ParseException($"Unexpected character '{c}' at position {_i}");
    }

    public ReadOnlySpan<char> SliceIdentifier(Token t)
        => _s.Slice(t.Start, t.Length);

    private void SkipWs()
    {
        while (_i < _s.Length)
        {
            char c = _s[_i];
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n') _i++;
            else break;
        }
    }
}