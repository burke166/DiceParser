namespace DiceParser;

/// <summary>Caps resource usage during parsing and evaluation of dice expressions.</summary>
/// <param name="MaxNodes">Maximum AST nodes allowed in a single expression.</param>
/// <param name="MaxDicePerExpr">Maximum number of dice that may be rolled in one expression.</param>
/// <param name="MaxSides">Maximum number of sides on a standard die (<c>dN</c>).</param>
/// <param name="MaxCustomDieFaces">Maximum number of faces on a custom die.</param>
/// <param name="MaxProgramExprs">Maximum number of expressions allowed in a program (semicolon-separated).</param>
/// <param name="MaxRollGroupEntries">Maximum entries in a roll group.</param>
/// <param name="MaxRollGroupLabelLength">Maximum length of a roll group label.</param>
public readonly record struct Limits(
    int MaxNodes,
    int MaxDicePerExpr,
    int MaxSides,
    int MaxCustomDieFaces,
    int MaxProgramExprs,
    int MaxRollGroupEntries,
    int MaxRollGroupLabelLength)
{
    /// <summary>Default limits suitable for typical interactive and library use.</summary>
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