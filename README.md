# DiceParser

DiceParser is a flexible, extensible dice expression parser and evaluator written in C#. It is designed as a **general-purpose dice engine** suitable for tabletop RPGs, simulations, and custom game systems.

The library supports a rich grammar for dice expressions, including exploding dice, rerolls, success counting, custom dice, and more—while remaining system-agnostic.

---

## ✨ Features

### 🎲 Core Dice Mechanics
- `XdY` — Roll X dice with Y sides  
  - Example: `3d10`
- Full arithmetic support  
  - Example: `(1+2)d(4+2)`

### 🎯 Multiple Expressions
- Separate expressions with `;`  
  - Example: `1d20; 2d6+3`

### 🎲 Custom Dice
- Arbitrary face dice:
  - `Xd{A,B,C}`
  - Example: `4d{-1,0,1}`

### 🎲 Fudge / Fate Dice
- `dF` is supported as a shorthand for Fudge/Fate dice
- Equivalent to: `d{-1,0,1}`
- Example:
  - `4dF` → rolls four dice returning -1, 0, or +1 each

### 💥 Exploding Dice
- `!` — explode into new dice
- `!!` — compound explosion
- `!p` — penetrating explosion
- `!>N` — explode on values ≥ N
- `!X` — explode only on X

### 🔁 Rerolls
- `rX` — reroll until not X
- `ro<3` — reroll once if condition is met

### 🎯 Success Counting
- `>=N` — count successes
- Example: `4d6>=5`

### 🧮 Keep / Drop
- `khN` — keep highest N
- `klN` — keep lowest N
- `dhN` — drop highest N
- `dlN` — drop lowest N

### 🧩 Inline Roll Groups
- Named roll sets:
  ```
  {attack: 1d20; damage: 2d6+3}
  ```

---

## 🧠 Evaluation Order

1. Roll base dice  
2. Apply rerolls  
3. Apply exploding dice  
4. Apply success counting  
5. Apply keep/drop  
6. Apply arithmetic  

---

## 🚀 Using DiceParser in Your Project

### Installation

Add the project or NuGet package (when published):

```bash
dotnet add package DiceParser
```

### Example Usage

```csharp
var parser = new DiceParser();
var result = parser.Evaluate("2d20kh1+5");

Console.WriteLine(result.Total);
```

### Structured Results

The evaluation returns:
- Total
- Individual rolls
- Success counts (if applicable)

---

## 🧰 CLI Tool

The repository includes **DiceParser.Cli**, which:
- Demonstrates usage of the library
- Provides a functional command-line dice roller

See the CLI README for details.

---

## 🎯 Design Philosophy

DiceParser is built around:
- General-purpose composable syntax
- Separation of engine vs. game logic
- Extensibility for new dice mechanics

Game-specific concepts (like advantage/disadvantage) should be implemented in consuming applications using core primitives like `kh` and `kl`.

---

## 📄 License

MIT
