using DiceParser;

namespace DiceParser.Test;

internal static class DiceExecuteExtensions
{
    internal static RollResult ExecuteSingleNumericRoll(this DiceEngine engine, string input, Limits? limits = null)
    {
        IReadOnlyList<ProgramExpressionResult> list = limits is null ? engine.Execute(input) : engine.Execute(input, limits);
        return Assert.IsType<NumericExpressionResult>(Assert.Single(list)).Roll;
    }

    internal static RollResult ExecuteWithRngSingleNumeric(this DiceEngine engine, string input, IDiceRandom rng, Limits? limits = null)
    {
        IReadOnlyList<ProgramExpressionResult> list = limits is null ? engine.ExecuteWithRng(input, rng) : engine.ExecuteWithRng(input, rng, limits);
        return Assert.IsType<NumericExpressionResult>(Assert.Single(list)).Roll;
    }
}
