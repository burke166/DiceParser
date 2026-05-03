using System.Runtime.InteropServices;

namespace DiceParser.Ast;

internal sealed class NodePool
{
    private readonly List<Node> _nodes;
    private readonly List<DiceMod> _diceMods = new(capacity: 8);

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

    /// <summary>1-based handle; 0 means no modifier.</summary>
    public int AddDiceMod(DiceMod mod)
    {
        _diceMods.Add(mod);
        return _diceMods.Count;
    }

    internal int DiceModCount => _diceMods.Count;

    internal DiceMod GetDiceModByHandle(int oneBasedHandle) => _diceMods[oneBasedHandle - 1];

    public ref readonly Node this[int id] => ref CollectionsMarshal.AsSpan(_nodes)[id];
}