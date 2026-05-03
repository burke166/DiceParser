using System.Runtime.InteropServices;

namespace DiceParser.Ast;

internal sealed class NodePool
{
    private readonly List<Node> _nodes;
    private readonly List<DiceRollMods> _diceRollMods = new(capacity: 8);

    public NodePool(int capacity)
    {
        _nodes = new List<Node>(capacity);
    }

    public int Count => _nodes.Count;

    public int Add(Node n)
    {
        int id = _nodes.Count;
        _nodes.Add(n);
        return id;
    }

    /// <summary>1-based handle; 0 means no modifiers.</summary>
    public int AddDiceRollMods(DiceRollMods mods)
    {
        _diceRollMods.Add(mods);
        return _diceRollMods.Count;
    }

    internal int DiceRollModsCount => _diceRollMods.Count;

    internal DiceRollMods GetDiceRollModsByHandle(int oneBasedHandle) => _diceRollMods[oneBasedHandle - 1];

    public ref readonly Node this[int id] => ref CollectionsMarshal.AsSpan(_nodes)[id];
}
