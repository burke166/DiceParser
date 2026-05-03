namespace DiceParser.Ast;

internal readonly struct Node
{
    public readonly NodeKind Kind;
    public readonly OpKind Op;

    // Meaning depends on Kind:
    // Number:     Value used, A/B unused
    // Unary:      A = rhs
    // Binary:     A = lhs, B = rhs
    // Dice:       A = countExpr, B = sidesExpr, C = mods handle (1-based index in NodePool dice-mods list; 0 = none)
    // CustomDice: A = countExpr, C = mods handle (not used yet), Faces = faces array
    public readonly int A;
    public readonly int B;
    public readonly int C;
    public readonly int[] Faces = {};
    public readonly int Value;

    private Node(NodeKind kind, OpKind op, int a, int b, int c, int value, int[]? faces = null)
        => (Kind, Op, A, B, C, Value, Faces) = (kind, op, a, b, c,  value, faces ?? Array.Empty<int>());

    public static Node Number(int value) => new(NodeKind.Number, 0, 0, 0, 0, value);
    public static Node Unary(OpKind op, int rhs) => new(NodeKind.Unary, op, rhs, 0, 0, 0);
    public static Node Binary(OpKind op, int lhs, int rhs) => new(NodeKind.Binary, op, lhs, rhs, 0, 0);
    public static Node Dice(int countExpr, int sidesExpr, int modsHandle = 0) => new(NodeKind.Dice, 0, countExpr, sidesExpr, modsHandle, 0);
    public static Node CustomDie(int[] faces) => new(NodeKind.CustomDie, 0, 0, 0, 0, 0, faces);
}