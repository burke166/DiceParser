using DiceParser;
using DiceParser.Exceptions;

namespace DiceParser.Cli;

public static class Program
{
    public static void Main()
    {
        var engine = new DiceEngine(seed: 12345);

        string input = string.Empty;

        while (input != "exit")
        {
            Console.Write("Enter a dice expression (or 'exit' to quit): ");
            input = Console.ReadLine() ?? string.Empty;
            if (input == "exit")
                break;
            try
            {
                var results = engine.Execute(input);
                Console.WriteLine($"Input: {input}");
                for (int i = 0; i < results.Count; i++)
                {
                    ProgramExpressionResult r = results[i];
                    switch (r)
                    {
                        case NumericExpressionResult n:
                            Console.WriteLine(
                                $"  Expr {i + 1}: Total={n.Roll.Total}, DiceRolled={n.Roll.DiceRolled}, Rolls=[{string.Join(",", n.Roll.Rolls)}]");
                            break;
                        case RollGroupExpressionResult g:
                            Console.WriteLine($"  Expr {i + 1}: Roll group ({g.Group.Results.Count} labels)");
                            foreach (LabeledRollResult lr in g.Group.Results)
                            {
                                Console.WriteLine(
                                    $"    {lr.Label}: Total={lr.Result.Total}, DiceRolled={lr.Result.DiceRolled}, Rolls=[{string.Join(",", lr.Result.Rolls)}]");
                            }

                            break;
                    }
                }
                Console.WriteLine();
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
            }
            catch (EvalException ex)
            {
                Console.WriteLine($"Evaluation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
