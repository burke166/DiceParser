using DiceParser.Ast;
using DiceParser.Exceptions;
using DiceParser.Lexing;

namespace DiceParser.Parsing;

internal ref struct Parser
{
    private Lexer _lex;
    private readonly NodePool _pool;
    private readonly Limits _limits;

    public Parser(ref Lexer lexer, NodePool pool, Limits limits)
    {
        _lex = lexer;
        _pool = pool;
        _limits = limits;
    }

    public List<int> ParseProgram()
    {
        var roots = new List<int>(capacity: 4);

        while (_lex.Current.Kind != TokenKind.End)
        {
            if (roots.Count >= _limits.MaxProgramExprs)
                throw new ParseException($"Program has too many expressions (max {_limits.MaxProgramExprs}).");

            int expr = ParseExpression(0);
            roots.Add(expr);

            if (_lex.Current.Kind == TokenKind.Semicolon)
            {
                _lex.Next();
                continue;
            }

            if (_lex.Current.Kind == TokenKind.End)
                break;

            throw new ParseException($"Expected ';' or end, found {_lex.Current.Kind}");
        }

        // Write back lexer state to caller (important since Lexer is a ref struct)
        return roots;
    }

    // Binding powers (Pratt)
    // Higher = binds tighter
    // Unary handled in prefix parse.
    private static int Lbp(TokenKind kind) => kind switch
    {
        TokenKind.D => 70,              // Dice binds tighter than * / %
        TokenKind.Star or TokenKind.Slash or TokenKind.Percent => 60,
        TokenKind.Plus or TokenKind.Minus => 50,
        _ => 0
    };

    private int ParseExpression(int minBp)
    {
        int left = ParsePrefix();

        while (true)
        {
            var tok = _lex.Current;
            int lbp = Lbp(tok.Kind);
            if (lbp < minBp) break;

            // Infix parse
            switch (tok.Kind)
            {
                case TokenKind.Plus:
                case TokenKind.Minus:
                case TokenKind.Star:
                case TokenKind.Slash:
                case TokenKind.Percent:
                    {
                        _lex.Next();
                        int rbp = lbp + 1; // left-associative
                        int right = ParseExpression(rbp);

                        var op = tok.Kind switch
                        {
                            TokenKind.Plus => OpKind.Add,
                            TokenKind.Minus => OpKind.Sub,
                            TokenKind.Star => OpKind.Mul,
                            TokenKind.Slash => OpKind.Div,
                            TokenKind.Percent => OpKind.Mod,
                            _ => throw new ParseException("Unreachable")
                        };

                        left = AddNode(Node.Binary(op, left, right));
                        break;
                    }
                case TokenKind.D:
                    {
                        // left 'd' right forms a Dice node
                        // Support implied count: if left is missing, we won't reach here; that is handled in prefix (d20).
                        _lex.Next();

                        int sides = ParseExpression(lbp + 1); // bind tightly to right
                        int modsHandle = ParseOptionalDiceMods();
                        left = AddNode(Node.Dice(left, sides, modsHandle));
                        break;
                    }
                default:
                    return left;
            }
        }

        return left;
    }

    private int ParseCustomDie()
    {
        var faces = new List<int>();

        if (_lex.Current.Kind == TokenKind.RBrace)
            throw new ParseException("Custom die must have at least one face.");

        do
        {
            if (_lex.Current.Kind != TokenKind.Number &&
                _lex.Current.Kind != TokenKind.Minus)
            {
                throw new ParseException($"Expected custom die face, found {_lex.Current.Kind}");
            }

            int face = ParseSignedIntegerLiteral();
            faces.Add(face);

            if (faces.Count > _limits.MaxCustomDieFaces)
                throw new ParseException($"Custom die has too many faces.");
        }
        while (TryConsume(TokenKind.Comma));

        Expect(TokenKind.RBrace);
        _lex.Next();

        return AddNode(Node.CustomDie(faces.ToArray()));
    }

    private int ParseSignedIntegerLiteral()
    {
        var sign = 1;

        if (_lex.Current.Kind == TokenKind.Minus)
        {
            sign = -1;
            _lex.Next();
        }
        else if (_lex.Current.Kind == TokenKind.Plus)
        {
            _lex.Next();
        }

        if (_lex.Current.Kind != TokenKind.Number)
            throw new ParseException($"Expected number, found {_lex.Current.Kind}");

        int value = _lex.Current.IntValue;
        _lex.Next();

        return sign * value;
    }

    private int ParsePrefix()
    {
        var tok = _lex.Current;
        switch (tok.Kind)
        {
            case TokenKind.Number:
                _lex.Next();
                return AddNode(Node.Number(tok.IntValue));

            case TokenKind.LParen:
                {
                    _lex.Next();
                    int inner = ParseExpression(0);
                    Expect(TokenKind.RParen);
                    _lex.Next();
                    return inner;
                }

            case TokenKind.LBrace:
                {
                    _lex.Next();
                    return ParseCustomDie();
                }

            case TokenKind.Plus:
            case TokenKind.Minus:
                {
                    _lex.Next();
                    int rhs = ParseExpression(80); // unary binds tight
                    var op = tok.Kind == TokenKind.Plus ? OpKind.Pos : OpKind.Neg;
                    return AddNode(Node.Unary(op, rhs));
                }

            case TokenKind.D:
                {
                    // Implied count: d20 == 1d20
                    _lex.Next();
                    int sides = ParseExpression(71); // right binds tight
                    int modsHandle = ParseOptionalDiceMods();
                    int one = AddNode(Node.Number(1));
                    return AddNode(Node.Dice(one, sides, modsHandle));
                }

            default:
                throw new ParseException($"Unexpected token {tok.Kind}");
        }
    }

    private void Expect(TokenKind kind)
    {
        if (_lex.Current.Kind != kind)
            throw new ParseException($"Expected {kind}, found {_lex.Current.Kind}");
    }

    private bool TryConsume(TokenKind kind)
    {
        if (_lex.Current.Kind != kind)
            return false;

        _lex.Next();
        return true;
    }

    private int AddNode(Node n)
    {
        int id = _pool.Add(n);
        if (_pool.Count > _limits.MaxNodes)
            throw new ParseException($"Expression too complex (max nodes {_limits.MaxNodes}).");
        return id;
    }

    /// <summary>Optional reroll (<c>r</c>/<c>ro</c>) before or after explode, optional explode, optional keep/drop, optional &gt;=N success count.</summary>
    private int ParseOptionalDiceMods()
    {
        RerollSpec prefixReroll = TryParseRerollSuffix();
        ExplodeSpec explode = ParseOptionalExplodePrefix();
        RerollSpec tailReroll = TryParseRerollSuffix();

        if (prefixReroll.HasReroll && tailReroll.HasReroll)
            throw new ParseException("Duplicate reroll modifier.");

        RerollSpec reroll = prefixReroll.HasReroll ? prefixReroll : tailReroll;

        DiceMod keepDrop = ParseOptionalKeepDropSuffix();
        (bool hasSuccess, int successAtLeast) = ParseOptionalSuccessCountSuffix();

        var bundle = new DiceRollMods(explode, reroll, keepDrop, hasSuccess, successAtLeast);
        if (bundle.IsEmpty)
            return 0;

        return _pool.AddDiceRollMods(bundle);
    }

    private RerollSpec TryParseRerollSuffix()
    {
        if (_lex.Current.Kind != TokenKind.Identifier)
            return default;

        ReadOnlySpan<char> id = _lex.SliceIdentifier(_lex.Current);
        bool once;
        if (id.Equals("ro", StringComparison.OrdinalIgnoreCase))
            once = true;
        else if (id.Equals("r", StringComparison.OrdinalIgnoreCase))
            once = false;
        else
            return default;

        _lex.Next();
        (RerollCompareKind cmp, int n) = ParseRerollCompareTail();
        return new RerollSpec(once, cmp, n);
    }

    private (RerollCompareKind Cmp, int N) ParseRerollCompareTail()
    {
        switch (_lex.Current.Kind)
        {
            case TokenKind.Greater:
                _lex.Next();
                return (RerollCompareKind.GreaterOrEqual, ParseSignedIntegerLiteral());
            case TokenKind.Less:
                _lex.Next();
                return (RerollCompareKind.LessOrEqual, ParseSignedIntegerLiteral());
            case TokenKind.Equal:
                _lex.Next();
                return (RerollCompareKind.Equal, ParseSignedIntegerLiteral());
            case TokenKind.Number:
                {
                    int n = _lex.Current.IntValue;
                    _lex.Next();
                    return (RerollCompareKind.Equal, n);
                }
            case TokenKind.Plus:
            case TokenKind.Minus:
                return (RerollCompareKind.Equal, ParseSignedIntegerLiteral());
            default:
                throw new ParseException("Expected reroll threshold after 'r' or 'ro'.");
        }
    }

    private ExplodeSpec ParseOptionalExplodePrefix()
    {
        ExplodeMode mode;
        switch (_lex.Current.Kind)
        {
            case TokenKind.ExplodeCompound:
                _lex.Next();
                mode = ExplodeMode.Compound;
                break;
            case TokenKind.ExplodeCompoundPenetrating:
                _lex.Next();
                mode = ExplodeMode.CompoundPenetrating;
                break;
            case TokenKind.ExplodePenetrating:
                _lex.Next();
                mode = ExplodeMode.Penetrating;
                break;
            case TokenKind.ExplodeStandard:
                _lex.Next();
                mode = ExplodeMode.Standard;
                break;
            default:
                return default;
        }

        return ParseExplodeCompareTail(mode);
    }

    private ExplodeSpec ParseExplodeCompareTail(ExplodeMode mode)
    {
        ExplodeCompareKind cmp = ExplodeCompareKind.EqualMax;
        int n = 0;

        switch (_lex.Current.Kind)
        {
            case TokenKind.Greater:
                _lex.Next();
                cmp = ExplodeCompareKind.GreaterOrEqualN;
                n = ParseSignedIntegerLiteral();
                break;
            case TokenKind.Less:
                _lex.Next();
                cmp = ExplodeCompareKind.LessOrEqualN;
                n = ParseSignedIntegerLiteral();
                break;
            case TokenKind.Number:
                cmp = ExplodeCompareKind.EqualN;
                n = _lex.Current.IntValue;
                _lex.Next();
                break;
            case TokenKind.Plus:
            case TokenKind.Minus:
                cmp = ExplodeCompareKind.EqualN;
                n = ParseSignedIntegerLiteral();
                break;
        }

        return new ExplodeSpec(mode, cmp, n);
    }

    private DiceMod ParseOptionalKeepDropSuffix()
    {
        if (_lex.Current.Kind != TokenKind.Identifier)
            return default;

        ReadOnlySpan<char> id = _lex.SliceIdentifier(_lex.Current);

        DiceModKind kind;
        if (id.Equals("kh", StringComparison.OrdinalIgnoreCase))
            kind = DiceModKind.KeepHighest;
        else if (id.Equals("kl", StringComparison.OrdinalIgnoreCase))
            kind = DiceModKind.KeepLowest;
        else if (id.Equals("dh", StringComparison.OrdinalIgnoreCase))
            kind = DiceModKind.DropHighest;
        else if (id.Equals("dl", StringComparison.OrdinalIgnoreCase))
            kind = DiceModKind.DropLowest;
        else
            throw new ParseException($"Unknown dice modifier '{new string(id)}'.");

        _lex.Next();

        if (_lex.Current.Kind != TokenKind.Number || _lex.Current.IntValue <= 0)
            throw new ParseException("Expected positive integer after keep/drop dice modifier.");

        int n = _lex.Current.IntValue;
        _lex.Next();

        return new DiceMod(kind, n);
    }

    /// <summary>Roll20-style <c>&gt;=N</c> after explode/keep-drop; not an arithmetic comparison.</summary>
    private (bool HasSuccess, int AtLeast) ParseOptionalSuccessCountSuffix()
    {
        if (_lex.Current.Kind != TokenKind.GreaterEqual)
            return (false, 0);

        _lex.Next();
        int n = ParseSignedIntegerLiteral();
        return (true, n);
    }
}