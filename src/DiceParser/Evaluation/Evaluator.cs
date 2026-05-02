using DiceParser.Ast;
using DiceParser.Exceptions;
using System.Text;

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

            NodeKind.CustomDie => EvalDice(n.A, n.B, ref ctx, n.Faces),

            _ => throw new EvalException("Unknown node kind")
        };
    }

    private int EvalDice(int countExprId, int sidesExprId, ref EvalContext ctx, int[]? faces = null)
    {
        int count = 1; // EvalInt(countExprId, ref ctx);
        int sides = faces == null ? EvalInt(sidesExprId, ref ctx) : faces.Length;

        if (count < 0) throw new EvalException("Dice count cannot be negative.");
        if (sides <= 0) throw new EvalException("Dice sides must be positive.");
        if (sides > _limits.MaxSides) throw new EvalException($"Dice sides too large (max {_limits.MaxSides}).");
        if (faces is not null && sides > _limits.MaxCustomDieFaces) throw new EvalException($"Too many custom die faces (max {_limits.MaxCustomDieFaces}).");
        if (ctx.DiceRolled + count > _limits.MaxDicePerExpr) throw new EvalException($"Too many dice rolled (max {_limits.MaxDicePerExpr}).");

        Console.WriteLine(count.ToString());

        int total = 0;

        for (int i = 0; i < count; i++)
        {
            int roll = faces is null ? ctx.Rng.NextInt(1, sides + 1) : faces[ctx.Rng.NextInt(0, sides)]; // inclusive range
            ctx.Rolls.Add(roll);
            total += roll;
        }

        ctx.DiceRolled += count;

        // TODO: apply modifiers (explode/reroll/keep-drop/etc.) using modsHandle from Node.C later.

        return total;
    }

    public string DumpTree(int root)
    {
        var sb = new StringBuilder();
        var visiting = new HashSet<int>();

        DumpNode(root, sb, "", true, visiting);

        return sb.ToString();
    }

    private void DumpNode(
        int id,
        StringBuilder sb,
        string indent,
        bool last,
        HashSet<int> visiting)
    {
        var branch = last ? "└─ " : "├─ ";

        if (!visiting.Add(id))
        {
            sb.AppendLine($"{indent}{branch}#{id} <cycle detected>");
            return;
        }

        var node = _pool[id];

        sb.AppendLine($"{indent}{branch}#{id} {FormatNode(node)}");

        var childIndent = indent + (last ? "   " : "│  ");

        switch (node.Kind)
        {
            case NodeKind.Number:
            case NodeKind.CustomDie:
                break;

            case NodeKind.Unary:
                DumpNode(node.A, sb, childIndent, true, visiting);
                break;

            case NodeKind.Binary:
            case NodeKind.Dice:
                DumpNode(node.A, sb, childIndent, false, visiting);
                DumpNode(node.B, sb, childIndent, true, visiting);
                break;
        }

        visiting.Remove(id);
    }

    private static string FormatNode(Node node)
    {
        return node.Kind switch
        {
            NodeKind.Number =>
                $"Number {node.Value}",

            NodeKind.CustomDie =>
                $"CustomDie [{string.Join(", ", node.Faces ?? Array.Empty<int>())}]",

            NodeKind.Unary =>
                $"Unary {node.Op}",

            NodeKind.Binary =>
                $"Binary {node.Op}",

            NodeKind.Dice =>
                "Dice",

            _ =>
                node.Kind.ToString()
        };
    }

}
