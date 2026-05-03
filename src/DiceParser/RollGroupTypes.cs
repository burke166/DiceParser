namespace DiceParser;

/// <summary>Outcome of evaluating a single labeled sub-expression inside a <see cref="RollGroupResult"/>.</summary>
public sealed record LabeledRollResult(string Label, RollResult Result);

/// <summary>Structured result for <c>{ label: expr, ... }</c>; not a single numeric total.</summary>
public sealed record RollGroupResult(IReadOnlyList<LabeledRollResult> Results);

/// <summary>One top-level program expression outcome (after splitting on <c>;</c>).</summary>
public abstract record ProgramExpressionResult;

/// <summary>Standard dice expression evaluated to a single total and roll log.</summary>
public sealed record NumericExpressionResult(RollResult Roll) : ProgramExpressionResult;

/// <summary>Labeled roll group container outcome.</summary>
public sealed record RollGroupExpressionResult(RollGroupResult Group) : ProgramExpressionResult;
