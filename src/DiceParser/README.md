# DiceParser (library)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../../LICENSE)

**DiceParser** is the core C# library: it lexes, parses, and evaluates dice expressions. Publish it as the NuGet package **DiceParser** and reference it from games, bots, web APIs, or any .NET app that needs a shared dice engine.

---

## 📦 Installation

```bash
dotnet add package DiceParser
```

The package targets **.NET 10** (`net10.0`). Use a matching target framework (or higher) in your project.

---

## 🚀 Quick start

```csharp
using DiceParser;

var engine = new DiceEngine(seed: 42);
IReadOnlyList<ProgramExpressionResult> results = engine.Execute("2d20kh1+5");

foreach (ProgramExpressionResult item in results)
{
    if (item is NumericExpressionResult n)
    {
        Console.WriteLine($"Total: {n.Roll.Total}");
        Console.WriteLine($"Dice rolled: {n.Roll.DiceRolled}");
        Console.WriteLine($"Rolls: [{string.Join(", ", n.Roll.Rolls)}]");
    }
    else if (item is RollGroupExpressionResult g)
    {
        foreach (LabeledRollResult lr in g.Group.Results)
            Console.WriteLine($"{lr.Label}: total {lr.Result.Total}");
    }
}
```

---

## 🎛️ API overview

### `DiceEngine`

- **`new DiceEngine(int seed)`** — Rolls use a fast, deterministic **xoshiro256\*\*** generator (see the [solution README](../../README.md) for a short description). The PRNG state advances across successive `Execute` calls on the same engine instance.
- **`DiceEngine.CreateCrypto()`** — Rolls use cryptographically strong randomness (`RandomNumberGenerator`). Use this when unpredictability matters more than reproducibility.

### `Execute`

```csharp
IReadOnlyList<ProgramExpressionResult> Execute(string input, Limits? limits = null);
```

- **`input`** — A full *program*: one or more expressions separated by `;`, for example `1d20; 2d6+3`.
- **`limits`** — Optional caps on parse/eval resources (AST size, dice count, sides, roll-group size, and so on). Omit it to use `Limits.Default`.

Results are returned in order. Each top-level item is either:

- **`NumericExpressionResult`** — Carries a **`RollResult`** with **`Total`**, **`DiceRolled`**, and **`Rolls`** (per-die values in order).
- **`RollGroupExpressionResult`** — Carries a **`RollGroupResult`** with labeled sub-results (`{label: expr, ...}` syntax).

### Exceptions

- **`ParseException`** — Invalid or unsupported syntax.
- **`EvalException`** — Evaluation failed (for example, limits exceeded or an invalid combination for a mechanic).

---

## 📚 Expression grammar

Supported operators (exploding dice, rerolls, success counting, custom faces, Fudge dice, keep/drop, roll groups, and arithmetic) are summarized in the [repository README](../../README.md).

---

## 📄 License

MIT — same as the parent DiceParser solution.
