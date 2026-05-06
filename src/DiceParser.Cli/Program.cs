using System.Globalization;
using System.Reflection;
using DiceParser;
using DiceParser.Diagnostics;
using DiceParser.Exceptions;

namespace DiceParser.Cli;

public static class Program
{
    private const int DefaultSeed = 12345;
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;

    public static int Main(string[] args) => Run(args);

    internal static int Run(string[] args)
    {
        if (!TryParseCliArgs(args, out var parsed, out var parseError))
        {
            Console.Error.WriteLine(parseError);
            return ExitFailure;
        }

        switch (parsed.Mode)
        {
            case CliRunMode.Help:
                PrintHelp();
                return ExitSuccess;
            case CliRunMode.Version:
                PrintVersion();
                return ExitSuccess;
            case CliRunMode.Error:
                Console.Error.WriteLine(parsed.ErrorMessage);
                return ExitFailure;
        }

        DiceEngine engine = CreateDiceEngine(parsed.UseCrypto, parsed.Seed);

        if (parsed.Mode == CliRunMode.RunOnce)
        {
            return RunOnce(engine, parsed.Expression!, parsed.Verbose);
        }

        RunInteractive(engine, parsed.Verbose);
        return ExitSuccess;
    }

    internal static bool TryParseCliArgs(string[] args, out ParsedCli parsed, out string? error)
    {
        parsed = default;
        error = null;

        bool useCrypto = false;
        bool seedSpecified = false;
        int? seed = null;
        bool help = false;
        bool version = false;
        bool verbose = false;
        var positional = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--")
            {
                positional.AddRange(args.AsSpan(i + 1).ToArray());
                break;
            }

            if (a == "-c" || a == "--crypto")
            {
                useCrypto = true;
                continue;
            }

            if (a == "-h" || a == "--help")
            {
                help = true;
                continue;
            }

            if (a == "--version")
            {
                version = true;
                continue;
            }

            if (a == "-v" || a == "--verbose")
            {
                verbose = true;
                continue;
            }

            if (a == "-s" || a == "--seed")
            {
                if (i + 1 >= args.Length)
                {
                    error = "Option '--seed' requires an integer value.";
                    return false;
                }

                string seedToken = args[++i];
                if (!int.TryParse(seedToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out int s))
                {
                    error = $"Invalid seed '{seedToken}'. Expected a 32-bit integer (e.g. --seed 12345).";
                    return false;
                }

                seed = s;
                seedSpecified = true;
                continue;
            }

            if (a.StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Unknown option '{a}'. See --help for usage.";
                return false;
            }

            if (a.StartsWith('-') && a.Length >= 2)
            {
                if (char.IsDigit(a[1]) || a[1] == '.')
                {
                    positional.Add(a);
                    continue;
                }

                if (a[1] == '(')
                {
                    positional.Add(a);
                    continue;
                }

                error = $"Unknown option '{a}'. See --help for usage.";
                return false;
            }

            positional.Add(a);
        }

        if (help)
        {
            parsed = new ParsedCli { Mode = CliRunMode.Help };
            return true;
        }

        if (version)
        {
            parsed = new ParsedCli { Mode = CliRunMode.Version };
            return true;
        }

        if (useCrypto && seedSpecified)
        {
            parsed = new ParsedCli
            {
                Mode = CliRunMode.Error,
                ErrorMessage = "Options --crypto and --seed cannot be used together.",
            };
            return true;
        }

        string? expression = positional.Count == 0 ? null : string.Join(' ', positional);

        if (expression is null)
        {
            parsed = new ParsedCli
            {
                Mode = CliRunMode.RunInteractive,
                UseCrypto = useCrypto,
                Seed = seedSpecified ? seed : null,
                Verbose = verbose,
            };
            return true;
        }

        parsed = new ParsedCli
        {
            Mode = CliRunMode.RunOnce,
            UseCrypto = useCrypto,
            Seed = seedSpecified ? seed : null,
            Expression = expression,
            Verbose = verbose,
        };
        return true;
    }

    internal readonly struct ParsedCli
    {
        public CliRunMode Mode { get; init; }
        public string? ErrorMessage { get; init; }
        public bool UseCrypto { get; init; }
        public int? Seed { get; init; }
        public string? Expression { get; init; }
        public bool Verbose { get; init; }
    }

    internal enum CliRunMode
    {
        Help,
        Version,
        RunInteractive,
        RunOnce,
        Error,
    }

    private static DiceEngine CreateDiceEngine(bool useCrypto, int? seed)
    {
        if (useCrypto)
            return DiceEngine.CreateCrypto();
        return new DiceEngine(seed ?? DefaultSeed);
    }

    private static void PrintHelp()
    {
        string name = GetExecutableDisplayName();
        Console.WriteLine(
            $"""
            Usage:
              {name} [options] [expression]

            Evaluate dice expressions from the command line, or run with no expression for an interactive session.

            Arguments:
              expression              Dice expression to evaluate. Use quotes if the expression contains spaces.

            Options:
              -c, --crypto            Use CryptoDiceRandom instead of XoshiroDiceRandom.
              -s, --seed <seed>       Use the specified integer seed with XoshiroDiceRandom.
              -v, --verbose           Print parsed AST diagnostics before each expression result.
              -h, --help              Show help and exit.
                  --version           Show version and exit.

            Examples:
              {name}
              {name} 4d6kh3
              {name} "1d20 + 5"
              {name} --seed 12345 "4d6kh3"
              {name} --crypto "1d100"
            """);
    }

    private static void PrintVersion()
    {
        Assembly asm = typeof(Program).Assembly;

        string? version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? asm.GetName().Version?.ToString()
            ?? "0.0.0";
        Console.WriteLine($"{GetExecutableDisplayName()} {version}");
    }

    private static string GetExecutableDisplayName()
    {
        return typeof(Program).Assembly.GetName().Name ?? "DiceParser.Cli";
    }

    private static int RunOnce(DiceEngine engine, string input, bool verbose)
    {
        try
        {
            var results = engine.Execute(input);
            PrintExpressionResults(input, results, verbose);
            return ExitSuccess;
        }
        catch (ParseException ex)
        {
            Console.WriteLine($"Parse error: {ex.Message}");
            return ExitFailure;
        }
        catch (EvalException ex)
        {
            Console.WriteLine($"Evaluation error: {ex.Message}");
            return ExitFailure;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return ExitFailure;
        }
    }

    private static void RunInteractive(DiceEngine engine, bool verbose)
    {
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
                PrintExpressionResults(input, results, verbose);
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

    private static void PrintExpressionResults(string input, IReadOnlyList<ProgramExpressionResult> results, bool verbose)
    {
        IReadOnlyList<string>? astDumps = null;
        if (verbose)
            astDumps = AstDumper.DumpProgram(input);

        Console.WriteLine($"Input: {input}");
        for (int i = 0; i < results.Count; i++)
        {
            if (verbose && astDumps is not null)
            {
                Console.WriteLine($"  Expr {i + 1} AST:");
                foreach (string line in astDumps[i].Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                    Console.WriteLine($"    {line}");
            }

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
    }
}
