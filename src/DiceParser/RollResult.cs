namespace DiceParser;

/// <summary>Outcome of rolling dice for a single numeric expression result.</summary>
/// <param name="Total">The final numeric total for the expression.</param>
/// <param name="DiceRolled">How many individual dice were rolled.</param>
/// <param name="Rolls">The value rolled on each die, in order.</param>
public readonly record struct RollResult(int Total, int DiceRolled, int[] Rolls);
