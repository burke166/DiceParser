# DiceParser.Cli

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../../LICENSE)

**DiceParser.Cli** is the command-line front end for the DiceParser library. It is published as a **.NET global tool**: install the NuGet package **DiceParser.Cli** and run the **`diceparser`** command. The tool is useful for quick rolls at the terminal and as a minimal example of calling **`DiceEngine`**.

---

## 📦 Install / update

Install globally (pick a stable version when you publish):

```bash
dotnet tool install -g DiceParser.Cli
```

Update an existing installation:

```bash
dotnet tool update -g DiceParser.Cli
```

---

## 🚀 Usage

```text
diceparser [options] [expression]
```

- **No expression** — Starts an **interactive** session: enter expressions until you type `exit`.
- **Expression present** — Parses and evaluates it once, prints results, then exits.

If the expression contains spaces, quote it.

### Options

| Option | Meaning |
|--------|---------|
| `-c`, `--crypto` | Use cryptographic randomness instead of the default PRNG. Cannot be combined with `--seed`. |
| `-s`, `--seed <seed>` | 32-bit integer seed for the default **xoshiro256\*\***-backed roller. |
| `-h`, `--help` | Print usage and exit. |
| `-v`, `--version` | Print version and exit. |

### Examples

```bash
diceparser
diceparser 4d6kh3
diceparser "1d20 + 5"
diceparser --seed 12345 "4d6kh3"
diceparser --crypto "1d100"
```

---

## 🎲 Supported syntax

The CLI accepts the same expressions as the library. In brief:

- **Core:** `XdY`, arithmetic, `;` between expressions  
- **Fudge:** `dF`  
- **Custom faces:** `d{a,b,c}`  
- **Exploding:** `!`, `!!`, `!p`, `!>N`, `!X`  
- **Rerolls:** `r…`, `ro…`  
- **Successes:** `>=N`  
- **Keep/drop:** `kh` / `kl` / `dh` / `dl`  
- **Roll groups** (comma-separated entries inside `{ }`):

```bash
diceparser "{attack:1d20,damage:2d6+3}"
```

Full feature notes and evaluation order live in the [solution README](../../README.md).

---

## 📤 Output

For each run the tool prints the input line, then one block per top-level expression:

- **Numeric expression** — `Total`, `DiceRolled`, and ordered `Rolls` for that expression.  
- **Roll group** — A short header plus each **label** with its own `Total`, `DiceRolled`, and `Rolls`.

---

## 🛠️ Development (run from source)

From the `src/DiceParser.Cli` project directory:

```bash
dotnet run
dotnet run -- "4d6kh3"
```

---

## 📄 License

Same as the parent DiceParser project (MIT).
