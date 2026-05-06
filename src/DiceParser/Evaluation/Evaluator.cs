using DiceParser;
using DiceParser.Ast;
using DiceParser.Exceptions;
using System.Text;

namespace DiceParser.Evaluation;

internal sealed class Evaluator
{
    private const int MaxRerollIterations = 100;

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

            NodeKind.RollGroup =>
                throw new EvalException("A roll group cannot be used as a number inside an expression."),

            _ => throw new EvalException("Unknown node kind")
        };
    }

    internal RollGroupResult EvalRollGroup(int nodeId, ref EvalContext ctx)
    {
        ref readonly Node n = ref _pool[nodeId];
        if (n.Kind != NodeKind.RollGroup)
            throw new EvalException("Internal error: expected roll group node.");

        ReadOnlySpan<RollGroupEntry> entries = _pool.GetRollGroupEntries(n.A, n.B);
        var labeled = new List<LabeledRollResult>(entries.Length);

        for (int i = 0; i < entries.Length; i++)
        {
            RollGroupEntry e = entries[i];
            var sub = new EvalContext(ctx.Rng, ctx.Limits);
            int total = EvalInt(e.ExprId, ref sub);
            labeled.Add(new LabeledRollResult(
                e.Label,
                new RollResult(total, sub.DiceRolled, sub.Rolls.ToArray())));
        }

        return new RollGroupResult(labeled);
    }

    private int EvalDice(int countExprId, int dieExprId, int modsHandle, ref EvalContext ctx)
    {
        int count = EvalInt(countExprId, ref ctx);
        int[] faces = EvalDieFaces(dieExprId, ref ctx);

        ValidateDice(count, faces);

        DiceRollMods mods = GetAndValidateMods(modsHandle, faces);
        List<int> segment = RollDiceSegment(count, faces, mods, ref ctx);

        int rollStart = ctx.Rolls.Count;
        ctx.Rolls.AddRange(segment);

        return ScoreDice(ctx.Rolls, rollStart, segment.Count, mods);
    }

    private void ValidateDice(int count, int[] faces)
    {
        if (count < 0)
            throw new EvalException("Dice count cannot be negative.");

        if (faces.Length == 0)
            throw new EvalException("Dice must have at least one face.");

        if (faces.Length > _limits.MaxSides)
            throw new EvalException($"Dice sides too large (max {_limits.MaxSides}).");
    }

    private DiceRollMods GetAndValidateMods(int modsHandle, ReadOnlySpan<int> faces)
    {
        if (modsHandle == 0)
            return default;

        if (modsHandle < 1 || modsHandle > _pool.DiceRollModsCount)
            throw new EvalException("Invalid dice modifier handle.");

        DiceRollMods mods = _pool.GetDiceRollModsByHandle(modsHandle);

        if (mods.Explode.HasExplode)
            ValidateExplodeCompare(mods.Explode, faces);

        if (mods.Reroll.HasReroll)
            ValidateContinuousRerollLessOrEqual(mods.Reroll, faces);

        return mods;
    }

    private List<int> RollDiceSegment(
        int count,
        ReadOnlySpan<int> faces,
        DiceRollMods mods,
        ref EvalContext ctx)
    {
        var segment = new List<int>(Math.Max(count * 2, 8));
        RerollSpec reroll = mods.Reroll;

        if (!mods.Explode.HasExplode)
        {
            for (int i = 0; i < count; i++)
                segment.Add(RollWithRerolls(ref ctx, faces, reroll));

            return segment;
        }

        RollExplodingDiceSegment(count, faces, mods.Explode, reroll, segment, ref ctx);
        return segment;
    }

    private void RollExplodingDiceSegment(
        int count,
        ReadOnlySpan<int> faces,
        ExplodeSpec spec,
        RerollSpec reroll,
        List<int> segment,
        ref EvalContext ctx)
    {
        for (int i = 0; i < count; i++)
        {
            switch (spec.Mode)
            {
                case ExplodeMode.Standard:
                    AppendStandardExplodingChain(segment, ref ctx, faces, spec, reroll, penetrating: false);
                    break;

                case ExplodeMode.Penetrating:
                    AppendStandardExplodingChain(segment, ref ctx, faces, spec, reroll, penetrating: true);
                    break;

                case ExplodeMode.Compound:
                    segment.Add(RollCompoundExplodingChain(ref ctx, faces, spec, reroll, penetrating: false));
                    break;

                case ExplodeMode.CompoundPenetrating:
                    segment.Add(RollCompoundExplodingChain(ref ctx, faces, spec, reroll, penetrating: true));
                    break;

                default:
                    throw new EvalException("Unknown explode mode.");
            }
        }
    }

    private static int ScoreDice(List<int> rolls, int rollStart, int finalCount, DiceRollMods mods)
    {
        DiceMod kd = mods.KeepDrop;

        if (kd.Kind != DiceModKind.None)
            ValidateKeepDrop(kd, finalCount);

        if (mods.HasSuccessCount)
        {
            return kd.Kind == DiceModKind.None
                ? CountSuccessesInRange(rolls, rollStart, finalCount, mods.SuccessAtLeast)
                : CountKeepDropSuccesses(rolls, rollStart, finalCount, kd, mods.SuccessAtLeast);
        }

        return kd.Kind == DiceModKind.None
            ? SumRollRange(rolls, rollStart, finalCount)
            : SumKeepDrop(rolls, rollStart, finalCount, kd);
    }

    private static void ValidateKeepDrop(DiceMod kd, int finalCount)
    {
        if (kd.N <= 0)
            throw new EvalException("Keep/drop count must be positive.");

        if (kd.N > finalCount)
            throw new EvalException("Keep/drop count cannot exceed number of dice rolled.");
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

    private static bool RerollConditionMatches(int roll, RerollSpec spec)
    {
        if (!spec.HasReroll)
            return false;

        return spec.Compare switch
        {
            RerollCompareKind.Equal => roll == spec.N,
            RerollCompareKind.LessOrEqual => roll <= spec.N,
            RerollCompareKind.GreaterOrEqual => roll >= spec.N,
            _ => false
        };
    }

    /// <summary>Roll one face, then apply reroll (<c>r</c>/<c>ro</c>) before any explode handling sees the value.</summary>
    private int RollWithRerolls(ref EvalContext ctx, ReadOnlySpan<int> faces, RerollSpec spec)
    {
        int roll = RollOneDie(ref ctx, faces);
        if (!spec.HasReroll)
            return roll;

        if (spec.Once)
        {
            if (RerollConditionMatches(roll, spec))
                roll = RollOneDie(ref ctx, faces);
            return roll;
        }

        int iterations = 0;
        while (RerollConditionMatches(roll, spec))
        {
            if (++iterations > MaxRerollIterations)
                throw new EvalException($"Reroll limit exceeded (max {MaxRerollIterations} replacements per die).");

            roll = RollOneDie(ref ctx, faces);
        }

        return roll;
    }

    private void AppendStandardExplodingChain(
        List<int> segment,
        ref EvalContext ctx,
        ReadOnlySpan<int> faces,
        ExplodeSpec spec,
        RerollSpec reroll,
        bool penetrating)
    {
        int roll = RollWithRerolls(ref ctx, faces, reroll);
        segment.Add(roll);

        while (ShouldExplode(roll, faces, spec))
        {
            roll = RollWithRerolls(ref ctx, faces, reroll);
            int add = penetrating ? roll - 1 : roll;
            segment.Add(add);
        }
    }

    /// <summary>
    /// Compounds one die and its explosion chain into a single value. When <paramref name="penetrating"/> is true,
    /// the first roll counts in full; each subsequent explosion roll adds <c>raw - 1</c> to the sum while raw values
    /// still control whether exploding continues.
    /// </summary>
    private int RollCompoundExplodingChain(ref EvalContext ctx, ReadOnlySpan<int> faces, ExplodeSpec spec, RerollSpec reroll, bool penetrating)
    {
        int sum = RollWithRerolls(ref ctx, faces, reroll);
        int lastRoll = sum;

        while (ShouldExplode(lastRoll, faces, spec))
        {
            lastRoll = RollWithRerolls(ref ctx, faces, reroll);
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

    /// <summary>Continuous <c>r&lt;N</c> means reroll while <c>roll ≤ N</c>; if <c>N</c> is at least the die’s highest face, every outcome matches and rerolls never stop.</summary>
    private static void ValidateContinuousRerollLessOrEqual(RerollSpec spec, ReadOnlySpan<int> faces)
    {
        if (!spec.HasReroll || spec.Once)
            return;

        if (spec.Compare != RerollCompareKind.LessOrEqual)
            return;

        int maxF = MaxFace(faces);
        if (spec.N >= maxF)
            throw new EvalException(
                $"Continuous reroll 'r<{spec.N}' cannot complete on this die (threshold must be less than the highest face value {maxF}).");
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

    private static int CountSuccessesInRange(List<int> rolls, int start, int count, int atLeast)
    {
        int c = 0;
        for (int i = 0; i < count; i++)
        {
            if (rolls[start + i] >= atLeast)
                c++;
        }

        return c;
    }

    /// <summary>Same kept/dropped slice as <see cref="SumKeepDrop"/>; counts entries with value &gt;= <paramref name="atLeast"/>.</summary>
    private static int CountKeepDropSuccesses(List<int> rolls, int start, int count, DiceMod mod, int atLeast)
    {
        if (count == 0)
            return 0;

        if (mod.Kind == DiceModKind.None)
            return CountSuccessesInRange(rolls, start, count, atLeast);

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
                return CountOrderedPrefixSuccesses(rolls, start, order, mod.N, atLeast);
            case DiceModKind.KeepLowest:
                order.Sort(CompareKl);
                return CountOrderedPrefixSuccesses(rolls, start, order, mod.N, atLeast);
            case DiceModKind.DropHighest:
                order.Sort(CompareKh);
                return CountOrderedSuffixSuccesses(rolls, start, order, mod.N, atLeast);
            case DiceModKind.DropLowest:
                order.Sort(CompareKl);
                return CountOrderedSuffixSuccesses(rolls, start, order, mod.N, atLeast);
            default:
                return CountSuccessesInRange(rolls, start, count, atLeast);
        }
    }

    private static int CountOrderedPrefixSuccesses(List<int> rolls, int start, ReadOnlySpan<int> order, int take, int atLeast)
    {
        int c = 0;
        for (int i = 0; i < take; i++)
        {
            if (rolls[start + order[i]] >= atLeast)
                c++;
        }

        return c;
    }

    private static int CountOrderedSuffixSuccesses(List<int> rolls, int start, ReadOnlySpan<int> order, int drop, int atLeast)
    {
        int c = 0;
        for (int i = drop; i < order.Length; i++)
        {
            if (rolls[start + order[i]] >= atLeast)
                c++;
        }

        return c;
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
}
