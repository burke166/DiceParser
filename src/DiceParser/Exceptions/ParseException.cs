namespace DiceParser.Exceptions;

public sealed class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}