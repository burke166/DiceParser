using DiceParser.Ast;
using DiceParser.Exceptions;

namespace DiceParser.Evaluation;

internal sealed class Evaluator
{
    private readonly NodePool _pool;
    private readonly Limits _limits;

    public Evaluator(NodePool pool, Limits limits)
    {
        _pool = pool;
        _limits = limits;
    }

    public int EvalInt(int nodeId, ref EvalContext ctx)
    {
        ref readonly var n = ref _pool[nodeId];

        return n.Kind switch
        {
            NodeKind.Number => n.Value,

            NodeKind.Unary => n.Op switch
            {
                OpKind.Pos => EvalInt(n.A, ref ctx),
                OpKind.Neg => -EvalInt(n.A, ref ctx),
                _ => throw new EvalException("Unknown unary op")
            },

            NodeKind.Binary => n.Op switch
            {
                OpKind.Add => EvalInt(n.A, ref ctx) + EvalInt(n.B, ref ctx),
                OpKind.Sub => EvalInt(n.A, ref ctx) - EvalInt(n.B, ref ctx),
                OpKind.Mul => EvalInt(n.A, ref ctx) * EvalInt(n.B, ref ctx),
                OpKind.Div => EvalInt(n.A, ref ctx) / EvalInt(n.B, ref ctx),
                OpKind.Mod => EvalInt(n.A, ref ctx) % EvalInt(n.B, ref ctx),
                _ => throw new EvalException("Unknown binary op")
            },

            NodeKind.Dice => EvalDice(n.A, n.B, ref ctx),

            _ => throw new EvalException("Unknown node kind")
        };
    }

    private int EvalDice(int countExprId, int sidesExprId, ref EvalContext ctx)
    {
        int count = EvalInt(countExprId, ref ctx);
        int sides = EvalInt(sidesExprId, ref ctx);

        if (count < 0) throw new EvalException("Dice count cannot be negative.");
        if (sides <= 0) throw new EvalException("Dice sides must be positive.");
        if (sides > _limits.MaxSides) throw new EvalException($"Dice sides too large (max {_limits.MaxSides}).");
        if (ctx.DiceRolled + count > _limits.MaxDicePerExpr) throw new EvalException($"Too many dice rolled (max {_limits.MaxDicePerExpr}).");

        int total = 0;

        for (int i = 0; i < count; i++)
        {
            int roll = ctx.Rng.NextInt(1, sides + 1); // inclusive range
            ctx.Rolls.Add(roll);
            total += roll;
        }

        ctx.DiceRolled += count;

        // TODO: apply modifiers (explode/reroll/keep-drop/etc.) using modsHandle from Node.C later.

        return total;
    }
}
