namespace DiceParser.Ast;

public readonly struct CustomDieNode
{
    public readonly int[] Faces;

    public CustomDieNode(int[] faces)
    {
        Faces = faces;
    }
}