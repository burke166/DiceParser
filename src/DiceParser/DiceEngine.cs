using DiceParser.Ast;
using DiceParser.Evaluation;
using DiceParser.Lexing;
using DiceParser.Parsing;
using DiceParser.Random;

namespace DiceParser;

public sealed class DiceEngine
{
    public DiceEngine(int seed)
    {
        _useCrypto = false;
        _rng = new Xoshiro256StarStar((ulong)seed);
    }

    /// <summary>Uses <see cref="CryptoDiceRandom"/> for rolls (non-deterministic).</summary>
    public static DiceEngine CreateCrypto()
    {
        return new DiceEngine(crypto: true);
    }

    private DiceEngine(bool crypto)
    {
        _useCrypto = crypto;
        _rng = default;
    }

    private readonly bool _useCrypto;
    private Xoshiro256StarStar _rng;

    /// <summary>
    /// Parses and evaluates a program (one or more expressions separated by <c>;</c>).
    /// Each item is either a numeric <see cref="RollResult"/> or a structured <see cref="RollGroupResult"/>.
    /// </summary>
    public IReadOnlyList<ProgramExpressionResult> Execute(string input, Limits? limits = null)
    {
        limits ??= Limits.Default;

        var lexer = new Lexer(input.AsSpan());
        var pool = new NodePool(capacity: 128);
        var parser = new Parser(ref lexer, pool, limits.Value);

        var roots = parser.ParseProgram();
        var evaluator = new Evaluator(pool, limits.Value);

        var results = new List<ProgramExpressionResult>(roots.Count);
        if (_useCrypto)
        {
            var cryptoRng = new CryptoDiceRandom();
            foreach (int rootId in roots)
            {
                var ctx = new EvalContext(cryptoRng, limits.Value);
                results.Add(BuildProgramResult(rootId, pool, evaluator, ref ctx));
            }
        }
        else
        {
            foreach (int rootId in roots)
            {
                var diceRandom = new XoshiroDiceRandom(_rng);
                var ctx = new EvalContext(diceRandom, limits.Value);

                ProgramExpressionResult item = BuildProgramResult(rootId, pool, evaluator, ref ctx);
                _rng = diceRandom.State;
                results.Add(item);
            }
        }

        return results;
    }

    /// <summary>For tests: evaluate with a supplied RNG (does not advance the engine seed state).</summary>
    internal IReadOnlyList<ProgramExpressionResult> ExecuteWithRng(string input, IDiceRandom rng, Limits? limits = null)
    {
        limits ??= Limits.Default;

        var lexer = new Lexer(input.AsSpan());
        var pool = new NodePool(capacity: 128);
        var parser = new Parser(ref lexer, pool, limits.Value);

        var roots = parser.ParseProgram();
        var evaluator = new Evaluator(pool, limits.Value);

        var results = new List<ProgramExpressionResult>(roots.Count);
        foreach (int rootId in roots)
        {
            var ctx = new EvalContext(rng, limits.Value);
            results.Add(BuildProgramResult(rootId, pool, evaluator, ref ctx));
        }

        return results;
    }

    private static ProgramExpressionResult BuildProgramResult(
        int rootId,
        NodePool pool,
        Evaluator evaluator,
        ref EvalContext ctx)
    {
        ref readonly Node root = ref pool[rootId];

        if (root.Kind == NodeKind.RollGroup)
        {
            RollGroupResult group = evaluator.EvalRollGroup(rootId, ref ctx);
            return new RollGroupExpressionResult(group);
        }

        int value = evaluator.EvalInt(rootId, ref ctx);
        return new NumericExpressionResult(new RollResult(
            Total: value,
            DiceRolled: ctx.DiceRolled,
            Rolls: ctx.Rolls.ToArray()));
    }
}
