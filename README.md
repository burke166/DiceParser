# DiceParser

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**DiceParser** is a C# solution that provides a flexible dice expression parser and evaluator, packaged as a reusable library and a command-line tool. It is meant as a **general-purpose dice engine** for tabletop RPGs, simulations, and custom game systems.

The grammar covers exploding dice, rerolls, success counting, custom dice, Fudge/Fate dice, keep/drop, inline roll groups, and full arithmetic—without baking in any one RPG’s rules.

---

## 📦 What’s in this repository

| Path | Description |
|------|-------------|
| [`src/DiceParser`](src/DiceParser) | Core library, shipped as the NuGet package **DiceParser**. |
| [`src/DiceParser.Cli`](src/DiceParser.Cli) | Global **.NET tool** (package **DiceParser.Cli**, command `diceparser`) built on the library. |

- **Using the library in your code** → [`src/DiceParser/README.md`](src/DiceParser/README.md)  
- **Using the CLI / dotnet tool** → [`src/DiceParser.Cli/README.md`](src/DiceParser.Cli/README.md)

---

## ✨ Features

### 🎲 Core dice mechanics

- `XdY` — roll *X* dice with *Y* sides (example: `3d10`)
- Arithmetic on counts and sides (example: `(1+2)d(4+2)`)

### 🎯 Multiple expressions

- Separate with `;` (example: `1d20; 2d6+3`)

### 🎲 Custom dice

- Arbitrary faces: `Xd{A,B,C}` (example: `4d{-1,0,1}`)

### 🎲 Fudge / Fate dice

- `dF` is shorthand for `d{-1,0,1}` (example: `4dF`)

### 💥 Exploding dice

- `!`, `!!`, `!p`, `!>N`, `!X`

### 🔁 Rerolls

- `rX`, `ro<3`, and related forms

### 🎯 Success counting

- `>=N` (example: `4d6>=5`)

### 🧮 Keep / drop

- `khN`, `klN`, `dhN`, `dlN`

### 🧩 Inline roll groups

Named sets use **commas** between entries (semicolons are not allowed inside the braces):

```
{attack:1d20,damage:2d6+3}
```

---

## 🧠 Evaluation order

1. Roll base dice  
2. Apply rerolls  
3. Apply exploding dice  
4. Apply success counting  
5. Apply keep/drop  
6. Apply arithmetic  

---

## 🎲 Randomness: xoshiro256\*\*

By default the library drives rolls with **xoshiro256\*\*** (Vigna’s *xor/shift/rotate* family): a compact **256-bit state**, very fast 64-bit steps, and the **\*\*** output scrambler (a rotated multiply) that strengthens statistical quality for typical game and simulation use compared with simpler LCGs or raw `System.Random`.

A single 64-bit seed is expanded with **SplitMix64** into a full nonzero state so short seeds still produce well-spread initial states. Integer ranges for dice use **rejection sampling** over that generator so small ranges stay uniform.

For cases where cryptographic unpredictability is required, the engine also supports **`DiceEngine.CreateCrypto()`**, which uses OS-backed cryptographic randomness instead of xoshiro256\*\*.

---

## 🎯 Design philosophy

DiceParser favors:

- General-purpose, composable syntax  
- A clear split between the engine and game-specific rules  
- Room to grow new dice mechanics without forking the grammar for one system  

Conveniences like D&D-style advantage/disadvantage belong in the consuming app, using primitives such as `kh` / `kl`.

---

## 📄 License

MIT
