namespace DiceParser.Ast;

/// <summary>Optional explode + optional keep/drop + optional Roll20-style success counting (<c>&gt;=N</c>).</summary>
internal readonly struct DiceRollMods
{
    public readonly ExplodeSpec Explode;
    public readonly DiceMod KeepDrop;
    /// <summary>When true, the dice expression result is the count of final dice (after keep/drop) with value &gt;= <see cref="SuccessAtLeast"/>.</summary>
    public readonly bool HasSuccessCount;
    public readonly int SuccessAtLeast;

    public DiceRollMods(ExplodeSpec explode, DiceMod keepDrop, bool hasSuccessCount = false, int successAtLeast = 0)
    {
        Explode = explode;
        KeepDrop = keepDrop;
        HasSuccessCount = hasSuccessCount;
        SuccessAtLeast = successAtLeast;
    }

    public bool IsEmpty =>
        !Explode.HasExplode
        && KeepDrop.Kind == DiceModKind.None
        && !HasSuccessCount;
}
