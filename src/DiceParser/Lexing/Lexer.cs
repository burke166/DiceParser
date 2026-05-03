using DiceParser.Exceptions;
using System.Globalization;

namespace DiceParser.Lexing;

internal readonly struct LexerBookmark
{
    public readonly int Index;
    public readonly Token Current;

    public LexerBookmark(int index, Token current)
    {
        Index = index;
        Current = current;
    }
}

internal ref struct Lexer
{
    private ReadOnlySpan<char> _s;
    private int _i;
    private int _lexemeStart;

    public Lexer(ReadOnlySpan<char> s)
    {
        _s = s;
        _i = 0;
        Current = default;
        Next();
    }

    public Token Current { get; private set; }

    /// <summary>Index into <see cref="Source"/> for the first character of <see cref="Current"/> (or next unconsumed char when <see cref="TokenKind.End"/>).</summary>
    public int RawIndex => _i;

    public ReadOnlySpan<char> Source => _s;

    /// <summary>Sets the scan position and refreshes <see cref="Current"/> from that offset.</summary>
    public void ResyncAt(int index)
    {
        _i = index;
        Next();
    }

    public void Next()
    {
        SkipWs();
        if (_i >= _s.Length)
        {
            _lexemeStart = _i;
            Current = new Token(TokenKind.End);
            return;
        }

        _lexemeStart = _i;
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
            case ':': _i++; Current = new Token(TokenKind.Colon); return;
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

    internal void SkipWhitespace()
    {
        while (_i < _s.Length)
        {
            char c = _s[_i];
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n') _i++;
            else break;
        }
    }

    /// <summary>Bookmark the start index of <see cref="Current"/> (for rewinding to re-lex or raw-scan from the same character).</summary>
    public LexerBookmark Bookmark() => new(_lexemeStart, Current);

    public void Restore(LexerBookmark mark)
    {
        _i = mark.Index;
        Current = mark.Current;
    }

    /// <summary>
    /// From <paramref name="bookmark"/>, if the next non-whitespace content is <c>label:</c> (roll-group label rules),
    /// consumes through ':' and returns true. Otherwise restores the lexer and returns false.
    /// </summary>
    public bool TryConsumeRollGroupLabelAndColon(LexerBookmark bookmark, out string label)
    {
        Restore(bookmark);
        label = string.Empty;
        SkipWhitespace();

        if (_i >= _s.Length)
        {
            Restore(bookmark);
            return false;
        }

        char c0 = _s[_i];
        if (!char.IsAsciiLetter(c0) && c0 != '_')
        {
            Restore(bookmark);
            return false;
        }

        int start = _i;
        _i++;

        while (_i < _s.Length)
        {
            char c = _s[_i];
            if (char.IsAsciiLetter(c) || char.IsAsciiDigit(c) || c == '_' || c == '-')
            {
                _i++;
                continue;
            }

            break;
        }

        int labelEnd = _i;
        SkipWhitespace();

        if (_i >= _s.Length || _s[_i] != ':')
        {
            Restore(bookmark);
            return false;
        }

        label = new string(_s.Slice(start, labelEnd - start));
        _i++; // ':'
        Next();
        return true;
    }

    private void SkipWs() => SkipWhitespace();
}