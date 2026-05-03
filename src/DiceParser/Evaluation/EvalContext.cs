using DiceParser.Random;

namespace DiceParser.Evaluation;

internal struct EvalContext
{
    public IRng Rng;
    public readonly Limits Limits;
    public int DiceRolled;
    public List<int> Rolls;

    public EvalContext(IRng rng, Limits limits)
    {
        Rng = rng;
        Limits = limits;
        DiceRolled = 0;
        Rolls = new List<int>(capacity: 32);
    }
}