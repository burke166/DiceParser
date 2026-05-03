namespace DiceParser;

public readonly record struct Limits(
    int MaxNodes,
    int MaxDicePerExpr,
    int MaxSides,
    int MaxCustomDieFaces,
    int MaxProgramExprs,
    int MaxRollGroupEntries,
    int MaxRollGroupLabelLength)
{
    public static readonly Limits Default = new(
        MaxNodes: 4096,
        MaxDicePerExpr: 10_000,
        MaxSides: 1_000_000,
        MaxCustomDieFaces: 1_000,
        MaxProgramExprs: 128,
        MaxRollGroupEntries: 64,
        MaxRollGroupLabelLength: 128
    );
}