# DiceParser.Cli

DiceParser.Cli is a command-line interface for the DiceParser library. It serves both as:

- A practical dice rolling tool
- A reference implementation for integrating DiceParser

---

## 🚀 Usage

Run from the command line:

```bash
dotnet run -- "expression"
```

Example:

```bash
dotnet run -- "2d20kh1+5"
```

---

## 🎲 Supported Syntax

The CLI supports all DiceParser syntax:

### Core Dice
- `XdY` → `3d6`

### Fudge / Fate Dice
- `dF` → equivalent to `d{-1,0,1}`
- Example:
  ```bash
  dotnet run -- "4dF"
  ```

### Arithmetic
- `2d6+3`
- `(1+2)d(4+2)`

### Exploding Dice
- `!`, `!!`, `!p`, `!>N`, `!X`

### Rerolls
- `rX`
- `ro<3`

### Success Counting
- `>=N`

### Keep / Drop
- `kh`, `kl`, `dh`, `dl`

### Multiple Rolls
- Use `;`
  ```bash
  dotnet run -- "1d20;2d6+3"
  ```

### Named Groups
```bash
dotnet run -- "{attack:1d20;damage:2d6+3}"
```

---

## 📤 Output

The CLI displays:
- Total result
- Individual dice rolls
- Additional computed data (successes, etc.)

---

## 🧠 Notes

- The CLI is a thin wrapper around the DiceParser library
- All parsing and evaluation logic resides in the core library
- Use this project as a reference for building APIs or UI applications

---

## 🔧 Example

```bash
dotnet run -- "4d6kh3"
```

Output (example):

```
Rolls: [6, 2, 5, 3]
Kept: [6, 5, 3]
Total: 14
```

---

## 📄 License

Same as the parent DiceParser project
