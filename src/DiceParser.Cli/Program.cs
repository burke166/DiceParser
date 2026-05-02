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
                    var r = results[i];
                    Console.WriteLine($"  Expr {i + 1}: Total={r.Total}, DiceRolled={r.DiceRolled}, Rolls=[{string.Join(",", r.Rolls)}]");
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
