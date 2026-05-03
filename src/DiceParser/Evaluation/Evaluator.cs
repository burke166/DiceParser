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

            NodeKind.Dice => EvalDice(n.A, n.B, n.C, ref ctx),

            _ => throw new EvalException("Unknown node kind")
        };
    }

    private int EvalDice(int countExprId, int dieExprId, int modsHandle, ref EvalContext ctx)
    {
        int count = EvalInt(countExprId, ref ctx);
        int[] faces = EvalDieFaces(dieExprId, ref ctx);

        if (count < 0)
            throw new EvalException("Dice count cannot be negative.");

        if (faces.Length <= 0)
            throw new EvalException("Dice must have at least one face.");

        if (faces.Length > _limits.MaxSides)
            throw new EvalException($"Dice sides too large (max {_limits.MaxSides}).");

        if (ctx.DiceRolled + count > _limits.MaxDicePerExpr)
            throw new EvalException($"Too many dice rolled (max {_limits.MaxDicePerExpr}).");

        int rollStart = ctx.Rolls.Count;

        for (int i = 0; i < count; i++)
        {
            int faceIndex = ctx.Rng.NextInt(0, faces.Length);
            int roll = faces[faceIndex];
            ctx.Rolls.Add(roll);
        }

        ctx.DiceRolled += count;

        int total = SumRollRange(ctx.Rolls, rollStart, count);

        // TODO: apply modifiers (explode/reroll/keep-drop/etc.) using modsHandle from Node.C later.
        if (modsHandle != 0)
        {
            if (modsHandle < 1 || modsHandle > _pool.DiceModCount)
                throw new EvalException("Invalid dice modifier handle.");

            DiceMod mod = _pool.GetDiceModByHandle(modsHandle);

            if (mod.N <= 0)
                throw new EvalException("Keep/drop count must be positive.");

            if (mod.N > count)
                throw new EvalException("Keep/drop count cannot exceed number of dice rolled.");

            total = SumKeepDrop(ctx.Rolls, rollStart, count, mod);
        }

        return total;
    }

    private static int SumRollRange(List<int> rolls, int start, int count)
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
            sum += rolls[start + i];
        return sum;
    }

    /// <summary>
    /// Stable ordering: sort by value, break ties by lower original index (earlier roll ranks first
    /// for keep-highest and last for keep-lowest as appropriate).
    /// </summary>
    private static int SumKeepDrop(List<int> rolls, int start, int count, DiceMod mod)
    {
        if (count == 0)
            return 0;

        Span<int> order = stackalloc int[count];
        for (int i = 0; i < count; i++)
            order[i] = i;

        int CompareKh(int a, int b)
        {
            int va = rolls[start + a];
            int vb = rolls[start + b];
            int c = vb.CompareTo(va);
            return c != 0 ? c : a.CompareTo(b);
        }

        int CompareKl(int a, int b)
        {
            int va = rolls[start + a];
            int vb = rolls[start + b];
            int c = va.CompareTo(vb);
            return c != 0 ? c : a.CompareTo(b);
        }

        switch (mod.Kind)
        {
            case DiceModKind.KeepHighest:
                order.Sort(CompareKh);
                return SumOrderedPrefix(rolls, start, order, mod.N);
            case DiceModKind.KeepLowest:
                order.Sort(CompareKl);
                return SumOrderedPrefix(rolls, start, order, mod.N);
            case DiceModKind.DropHighest:
                order.Sort(CompareKh);
                return SumOrderedSuffix(rolls, start, order, mod.N);
            case DiceModKind.DropLowest:
                order.Sort(CompareKl);
                return SumOrderedSuffix(rolls, start, order, mod.N);
            default:
                return SumRollRange(rolls, start, count);
        }
    }

    private static int SumOrderedPrefix(List<int> rolls, int start, ReadOnlySpan<int> order, int take)
    {
        int sum = 0;
        for (int i = 0; i < take; i++)
            sum += rolls[start + order[i]];
        return sum;
    }

    private static int SumOrderedSuffix(List<int> rolls, int start, ReadOnlySpan<int> order, int drop)
    {
        int sum = 0;
        for (int i = drop; i < order.Length; i++)
            sum += rolls[start + order[i]];
        return sum;
    }

    private int[] EvalDieFaces(int dieExprId, ref EvalContext ctx)
    {
        ref readonly Node n = ref _pool[dieExprId];

        return n.Kind switch
        {
            NodeKind.CustomDie => n.Faces,

            _ => BuildStandardDieFaces(EvalInt(dieExprId, ref ctx))
        };
    }

    private static int[] BuildStandardDieFaces(int sides)
    {
        if (sides <= 0)
            throw new EvalException("Dice sides must be positive.");

        var faces = new int[sides];

        for (int i = 0; i < sides; i++)
            faces[i] = i + 1;

        return faces;
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
