using System.Text;
using DiceParser.Ast;
using DiceParser.Lexing;
using DiceParser.Parsing;

namespace DiceParser.Diagnostics;

/// <summary>
/// Produces a stable, human-readable tree dump of parsed dice expressions for diagnostics.
/// </summary>
public static class AstDumper
{
    /// <summary>
    /// Parses <paramref name="input"/> as a dice program and returns one AST dump per top-level expression segment.
    /// </summary>
    /// <param name="input">Dice expression program text.</param>
    /// <param name="limits">Optional parser limits. Uses <see cref="Limits.Default"/> when omitted.</param>
    /// <returns>Ordered AST dumps, matching expression order in the parsed program.</returns>
    public static IReadOnlyList<string> DumpProgram(string input, Limits? limits = null)
    {
        limits ??= Limits.Default;

        var lexer = new Lexer(input.AsSpan());
        var pool = new NodePool(capacity: 128);
        var parser = new Parser(ref lexer, pool, limits.Value);
        var roots = parser.ParseProgram();

        var dumps = new List<string>(roots.Count);
        foreach (int rootId in roots)
            dumps.Add(DumpSingle(pool, rootId));

        return dumps;
    }

    private static string DumpSingle(NodePool nodes, int rootId)
    {
        var sb = new StringBuilder();
        var visiting = new HashSet<int>();
        DumpNode(rootId, nodes, sb, "", true, visiting);
        return sb.ToString();
    }

    private static void DumpNode(
        int id,
        NodePool pool,
        StringBuilder sb,
        string indent,
        bool last,
        HashSet<int> visiting)
    {
        var branch = last ? "└─ " : "├─ ";

        if (!visiting.Add(id))
        {
            sb.AppendLine($"{indent}{branch}#{id} <cycle detected>");
            return;
        }

        var node = pool[id];

        sb.AppendLine($"{indent}{branch}#{id} {FormatNode(node)}");

        var childIndent = indent + (last ? "   " : "│  ");

        switch (node.Kind)
        {
            case NodeKind.Number:
            case NodeKind.CustomDie:
                break;

            case NodeKind.Unary:
                DumpNode(node.A, pool, sb, childIndent, true, visiting);
                break;

            case NodeKind.Binary:
            case NodeKind.Dice:
                DumpNode(node.A, pool, sb, childIndent, false, visiting);
                DumpNode(node.B, pool, sb, childIndent, true, visiting);
                break;

            case NodeKind.RollGroup:
                ReadOnlySpan<RollGroupEntry> rg = pool.GetRollGroupEntries(node.A, node.B);
                for (int i = 0; i < rg.Length; i++)
                {
                    bool lastEntry = i == rg.Length - 1;
                    sb.AppendLine($"{childIndent}{(lastEntry ? "└─ " : "├─ ")}{rg[i].Label}:");
                    DumpNode(rg[i].ExprId, pool, sb, childIndent + (lastEntry ? "   " : "│  "), true, visiting);
                }

                break;
        }

        visiting.Remove(id);
    }

    private static string FormatNode(Node node)
    {
        return node.Kind switch
        {
            NodeKind.Number =>
                $"Number {node.Value}",

            NodeKind.CustomDie =>
                $"CustomDie [{string.Join(", ", node.Faces ?? Array.Empty<int>())}]",

            NodeKind.Unary =>
                $"Unary {node.Op}",

            NodeKind.Binary =>
                $"Binary {node.Op}",

            NodeKind.Dice =>
                $"Dice mods#{node.C}",

            NodeKind.RollGroup =>
                $"RollGroup entries@{node.A} count={node.B}",

            _ =>
                node.Kind.ToString()
        };
    }    
}