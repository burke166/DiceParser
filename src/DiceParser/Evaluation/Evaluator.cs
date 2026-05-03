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
        int[] facesArr = EvalDieFaces(dieExprId, ref ctx);
        ReadOnlySpan<int> faces = facesArr;

        if (count < 0)
            throw new EvalException("Dice count cannot be negative.");

        if (faces.Length <= 0)
            throw new EvalException("Dice must have at least one face.");

        if (faces.Length > _limits.MaxSides)
            throw new EvalException($"Dice sides too large (max {_limits.MaxSides}).");

        DiceRollMods modsBundle = default;
        bool hasMods = modsHandle != 0;
        if (hasMods)
        {
            if (modsHandle < 1 || modsHandle > _pool.DiceRollModsCount)
                throw new EvalException("Invalid dice modifier handle.");

            modsBundle = _pool.GetDiceRollModsByHandle(modsHandle);
            if (modsBundle.Explode.HasExplode)
                ValidateExplodeCompare(modsBundle.Explode, faces);
        }

        int rollStart = ctx.Rolls.Count;
        var segment = new List<int>(Math.Max(count * 2, 8));

        bool explode = hasMods && modsBundle.Explode.HasExplode;
        if (!explode)
        {
            if (ctx.DiceRolled + count > _limits.MaxDicePerExpr)
                throw new EvalException($"Too many dice rolled (max {_limits.MaxDicePerExpr}).");

            for (int i = 0; i < count; i++)
                segment.Add(RollOneDie(ref ctx, faces));
        }
        else
        {
            ExplodeSpec spec = modsBundle.Explode;
            switch (spec.Mode)
            {
                case ExplodeMode.Standard:
                    for (int i = 0; i < count; i++)
                        AppendStandardExplodingChain(segment, ref ctx, faces, spec, penetrating: false);
                    break;
                case ExplodeMode.Compound:
                    for (int i = 0; i < count; i++)
                        segment.Add(RollCompoundExplodingChain(ref ctx, faces, spec, penetrating: false));
                    break;
                case ExplodeMode.CompoundPenetrating:
                    for (int i = 0; i < count; i++)
                        segment.Add(RollCompoundExplodingChain(ref ctx, faces, spec, penetrating: true));
                    break;
                case ExplodeMode.Penetrating:
                    for (int i = 0; i < count; i++)
                        AppendStandardExplodingChain(segment, ref ctx, faces, spec, penetrating: true);
                    break;
                default:
                    throw new EvalException("Unknown explode mode.");
            }
        }

        foreach (int v in segment)
            ctx.Rolls.Add(v);

        int finalCount = segment.Count;
        DiceMod kd = hasMods ? modsBundle.KeepDrop : default;

        int total;
        if (kd.Kind != DiceModKind.None)
        {
            if (kd.N <= 0)
                throw new EvalException("Keep/drop count must be positive.");

            if (kd.N > finalCount)
                throw new EvalException("Keep/drop count cannot exceed number of dice rolled.");

            total = SumKeepDrop(ctx.Rolls, rollStart, finalCount, kd);
        }
        else
        {
            total = SumRollRange(ctx.Rolls, rollStart, finalCount);
        }

        return total;
    }

    private void TryConsumeRollBudget(ref EvalContext ctx)
    {
        if (ctx.DiceRolled >= _limits.MaxDicePerExpr)
            throw new EvalException($"Too many dice rolled (max {_limits.MaxDicePerExpr}).");

        ctx.DiceRolled++;
    }

    private int RollOneDie(ref EvalContext ctx, ReadOnlySpan<int> faces)
    {
        TryConsumeRollBudget(ref ctx);
        int faceIndex = ctx.Rng.NextInt(0, faces.Length);
        return faces[faceIndex];
    }

    private void AppendStandardExplodingChain(
        List<int> segment,
        ref EvalContext ctx,
        ReadOnlySpan<int> faces,
        ExplodeSpec spec,
        bool penetrating)
    {
        int roll = RollOneDie(ref ctx, faces);
        segment.Add(roll);

        while (ShouldExplode(roll, faces, spec))
        {
            roll = RollOneDie(ref ctx, faces);
            int add = penetrating ? roll - 1 : roll;
            segment.Add(add);
        }
    }

    /// <summary>
    /// Compounds one die and its explosion chain into a single value. When <paramref name="penetrating"/> is true,
    /// the first roll counts in full; each subsequent explosion roll adds <c>raw - 1</c> to the sum while raw values
    /// still control whether exploding continues.
    /// </summary>
    private int RollCompoundExplodingChain(ref EvalContext ctx, ReadOnlySpan<int> faces, ExplodeSpec spec, bool penetrating)
    {
        int sum = RollOneDie(ref ctx, faces);
        int lastRoll = sum;

        while (ShouldExplode(lastRoll, faces, spec))
        {
            lastRoll = RollOneDie(ref ctx, faces);
            sum += penetrating ? lastRoll - 1 : lastRoll;
        }

        return sum;
    }

    private static bool ShouldExplode(int roll, ReadOnlySpan<int> faces, ExplodeSpec spec)
    {
        int maxF = MaxFace(faces);

        return spec.Compare switch
        {
            ExplodeCompareKind.EqualMax => roll == maxF,
            ExplodeCompareKind.EqualN => roll == spec.N,
            ExplodeCompareKind.GreaterOrEqualN => roll >= spec.N,
            ExplodeCompareKind.LessOrEqualN => roll <= spec.N,
            _ => false
        };
    }

    private static void ValidateExplodeCompare(ExplodeSpec spec, ReadOnlySpan<int> faces)
    {
        if (!spec.HasExplode)
            return;

        int minF = MinFace(faces);
        int maxF = MaxFace(faces);

        switch (spec.Compare)
        {
            case ExplodeCompareKind.EqualMax:
                return;
            case ExplodeCompareKind.EqualN:
            case ExplodeCompareKind.GreaterOrEqualN:
            case ExplodeCompareKind.LessOrEqualN:
                if (spec.N < minF || spec.N > maxF)
                    throw new EvalException($"Explode compare value must be between {minF} and {maxF} for this die.");
                break;
            default:
                return;
        }
    }

    private static int MinFace(ReadOnlySpan<int> faces)
    {
        int m = faces[0];
        for (int i = 1; i < faces.Length; i++)
        {
            if (faces[i] < m)
                m = faces[i];
        }

        return m;
    }

    private static int MaxFace(ReadOnlySpan<int> faces)
    {
        int m = faces[0];
        for (int i = 1; i < faces.Length; i++)
        {
            if (faces[i] > m)
                m = faces[i];
        }

        return m;
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

        if (mod.Kind == DiceModKind.None)
            return SumRollRange(rolls, start, count);

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
                $"Dice mods#{node.C}",

            _ =>
                node.Kind.ToString()
        };
    }
}
