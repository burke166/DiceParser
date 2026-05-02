using System.Runtime.InteropServices;

namespace DiceParser.Ast;

internal sealed class NodePool
{
    private readonly List<Node> _nodes;

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

    public ref readonly Node this[int id] => ref CollectionsMarshal.AsSpan(_nodes)[id];
}