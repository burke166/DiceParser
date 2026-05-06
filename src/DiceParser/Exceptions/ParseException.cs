namespace DiceParser.Exceptions;

/// <summary>Thrown when a dice expression cannot be parsed.</summary>
public sealed class ParseException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ParseException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public ParseException(string message) : base(message) { }
}