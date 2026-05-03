namespace DiceParser.Ast;

/// <summary>Optional explode + optional keep/drop for one dice node.</summary>
internal readonly struct DiceRollMods
{
    public readonly ExplodeSpec Explode;
    public readonly DiceMod KeepDrop;

    public DiceRollMods(ExplodeSpec explode, DiceMod keepDrop)
    {
        Explode = explode;
        KeepDrop = keepDrop;
    }

    public bool IsEmpty => !Explode.HasExplode && KeepDrop.Kind == DiceModKind.None;
}
