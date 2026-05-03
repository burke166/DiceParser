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
        foreach (int rootId in roots)
        {
            var adapter = new XoshiroRngAdapter(_rng);
            var ctx = new EvalContext(adapter, limits.Value);

            ProgramExpressionResult item = BuildProgramResult(rootId, pool, evaluator, ref ctx);
            _rng = adapter.State;
            results.Add(item);
        }

        return results;
    }

    /// <summary>For tests: evaluate with a supplied RNG (does not advance the engine seed state).</summary>
    internal IReadOnlyList<ProgramExpressionResult> ExecuteWithRng(string input, IRng rng, Limits? limits = null)
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
