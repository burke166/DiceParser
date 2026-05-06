namespace DiceParser.Exceptions;

/// <summary>Thrown when evaluation of a dice expression fails (e.g. invalid values or limits exceeded).</summary>
public sealed class EvalException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="EvalException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public EvalException(string message) : base(message) { }
}