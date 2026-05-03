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
        _rng = new Xoshiro256StarStar((ulong)seed);
    }

    private Xoshiro256StarStar _rng;

    /// <summary>
    /// Parses and evaluates a program (one or more expressions separated by ';').
    /// Returns one RollResult per expression.
    /// </summary>
    public IReadOnlyList<RollResult> Execute(string input, Limits? limits = null)
    {
        limits ??= Limits.Default;

        var lexer = new Lexer(input.AsSpan());
        var pool = new NodePool(capacity: 128);
        var parser = new Parser(ref lexer, pool, limits.Value);

        var roots = parser.ParseProgram(); // list of node ids
        var evaluator = new Evaluator(pool, limits.Value);

        var results = new List<RollResult>(roots.Count);
        foreach (var rootId in roots)
        {
            var ctx = new EvalContext(_rng, limits.Value);
            var value = evaluator.EvalInt(rootId, ref ctx);

            // Update RNG state so the engine is deterministic across calls but not fixed-per-expression
            _rng = ctx.Rng;

            results.Add(new RollResult(
                Total: value,
                DiceRolled: ctx.DiceRolled,
                Rolls: ctx.Rolls.ToArray()
            ));
        }

        return results;
    }
}