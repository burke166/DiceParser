namespace DiceParser.Ast;

/// <summary>AST node representing a die with arbitrary face values (custom die).</summary>
public readonly struct CustomDieNode
{
    /// <summary>The face values for this die, in order.</summary>
    public readonly int[] Faces;

    /// <summary>Initializes a new instance of the <see cref="CustomDieNode"/> struct.</summary>
    /// <param name="faces">The face values for this die.</param>
    public CustomDieNode(int[] faces)
    {
        Faces = faces;
    }
}