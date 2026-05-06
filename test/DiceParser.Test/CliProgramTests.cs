using DiceParser.Cli;
using static DiceParser.Cli.Program;

namespace DiceParser.Test;

public class CliProgramTests
{
    [Fact]
    public void Parse_empty_args_runs_interactive_with_default_seed()
    {
        Assert.True(TryParseCliArgs([], out var p, out var err));
        Assert.Null(err);
        Assert.Equal(CliRunMode.RunInteractive, p.Mode);
        Assert.False(p.UseCrypto);
        Assert.Null(p.Seed);
    }

    [Fact]
    public void Parse_expression_single_token_run_once()
    {
        Assert.True(TryParseCliArgs(["4d6kh3"], out var p, out _));
        Assert.Equal(CliRunMode.RunOnce, p.Mode);
        Assert.Equal("4d6kh3", p.Expression);
    }

    [Fact]
    public void Parse_expression_multiple_tokens_joined_with_spaces()
    {
        Assert.True(TryParseCliArgs(["1d20", "+", "5"], out var p, out _));
        Assert.Equal(CliRunMode.RunOnce, p.Mode);
        Assert.Equal("1d20 + 5", p.Expression);
    }

    [Fact]
    public void Parse_options_may_follow_expression()
    {
        Assert.True(TryParseCliArgs(["4d6", "--seed", "42"], out var p, out _));
        Assert.Equal(CliRunMode.RunOnce, p.Mode);
        Assert.Equal("4d6", p.Expression);
        Assert.Equal(42, p.Seed);
    }

    [Fact]
    public void Parse_double_dash_sends_rest_to_expression()
    {
        Assert.True(TryParseCliArgs(["--", "--not-a-flag"], out var p, out _));
        Assert.Equal(CliRunMode.RunOnce, p.Mode);
        Assert.Equal("--not-a-flag", p.Expression);
    }

    [Fact]
    public void Parse_help_mode()
    {
        Assert.True(TryParseCliArgs(["--help"], out var p, out _));
        Assert.Equal(CliRunMode.Help, p.Mode);
    }

    [Fact]
    public void Parse_version_mode()
    {
        Assert.True(TryParseCliArgs(["--version"], out var p, out _));
        Assert.Equal(CliRunMode.Version, p.Mode);
    }

    [Fact]
    public void Parse_verbose_mode()
    {
        Assert.True(TryParseCliArgs(["-v", "1d6"], out var p, out _));
        Assert.Equal(CliRunMode.RunOnce, p.Mode);
        Assert.True(p.Verbose);
        Assert.Equal("1d6", p.Expression);
    }

    [Fact]
    public void Parse_crypto_and_seed_is_error_mode()
    {
        Assert.True(TryParseCliArgs(["--crypto", "--seed", "1", "1d6"], out var p, out _));
        Assert.Equal(CliRunMode.Error, p.Mode);
        Assert.Contains("crypto", p.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("seed", p.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_unknown_long_option_fails_try_parse()
    {
        Assert.False(TryParseCliArgs(["--nope"], out _, out var err));
        Assert.NotNull(err);
        Assert.Contains("--help", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_unknown_short_option_fails_try_parse()
    {
        Assert.False(TryParseCliArgs(["-z"], out _, out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Parse_seed_without_value_fails_try_parse()
    {
        Assert.False(TryParseCliArgs(["--seed"], out _, out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Parse_invalid_seed_fails_try_parse()
    {
        Assert.False(TryParseCliArgs(["--seed", "abc"], out _, out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Run_once_success_returns_zero()
    {
        int code = Run(["--seed", "0", "1d1"]);
        Assert.Equal(0, code);
    }

    [Fact]
    public void Run_once_parse_error_returns_nonzero()
    {
        int code = Run(["@@@"]);
        Assert.NotEqual(0, code);
    }

    [Fact]
    public void Run_help_returns_zero()
    {
        int code = Run(["-h"]);
        Assert.Equal(0, code);
    }

    [Fact]
    public void Run_version_returns_zero()
    {
        int code = Run(["--version"]);
        Assert.Equal(0, code);
    }

    [Fact]
    public void Run_crypto_and_seed_returns_nonzero()
    {
        int code = Run(["--crypto", "-s", "1", "1d6"]);
        Assert.NotEqual(0, code);
    }

    [Fact]
    public void Run_verbose_prints_ast_before_each_expression_result()
    {
        TextWriter originalOut = Console.Out;
        var captured = new StringWriter();

        try
        {
            Console.SetOut(captured);
            int code = Run(["--seed", "123", "--verbose", "1d6;2d4"]);
            Assert.Equal(0, code);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        string output = captured.ToString();
        int ast1 = output.IndexOf("Expr 1 AST:", StringComparison.Ordinal);
        int res1 = output.IndexOf("Expr 1: Total=", StringComparison.Ordinal);
        int ast2 = output.IndexOf("Expr 2 AST:", StringComparison.Ordinal);
        int res2 = output.IndexOf("Expr 2: Total=", StringComparison.Ordinal);

        Assert.True(ast1 >= 0, "Expected AST header for expression 1.");
        Assert.True(res1 > ast1, "Expected expression 1 result after its AST output.");
        Assert.True(ast2 > res1, "Expected expression 2 AST after expression 1 result.");
        Assert.True(res2 > ast2, "Expected expression 2 result after its AST output.");
    }
}
