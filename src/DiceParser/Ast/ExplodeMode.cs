namespace DiceParser.Ast;

internal enum ExplodeMode : byte
{
    None = 0,
    Standard,
    Compound,
    Penetrating,
    /// <summary>Compound explode with penetrating adjustment on each added roll after the first in the chain.</summary>
    CompoundPenetrating,
}
