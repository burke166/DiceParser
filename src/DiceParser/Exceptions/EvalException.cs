namespace DiceParser.Exceptions;

public sealed class EvalException : Exception
{
    public EvalException(string message) : base(message) { }
}